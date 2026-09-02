using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Retaguarda.Data.Identity;
using Retaguarda.Shared;
using Retaguarda.Shared.Authorization;

namespace Retaguarda.Web.Infrastructure;

/// <summary>
/// Garante que o papel interno Admin tenha TODAS as permissões do catálogo.
///
/// Não é conveniência, é trava de segurança: sem isso, alguém tira uma permissão do Admin e ninguém
/// mais consegue administrar nada — nem para desfazer. Roda a cada boot e acrescenta o que falta,
/// então permissão nova (inclusive as do projeto derivado) já nasce concedida ao Admin.
/// </summary>
public static class RolePermissionSeeder
{
    public static async Task SeedAdminPermissionsAsync(IServiceProvider services)
    {
        var roleManager = services.GetRequiredService<RoleManager<ApplicationRole>>();
        var catalog = services.GetRequiredService<IPermissionCatalog>();
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(RolePermissionSeeder));

        var admin = await roleManager.FindByNameAsync(RetaguardaRoles.Admin);
        if (admin is null)
        {
            logger.LogWarning("Papel {Role} não encontrado — permissões não aplicadas.", RetaguardaRoles.Admin);
            return;
        }

        var existing = (await roleManager.GetClaimsAsync(admin))
            .Where(claim => claim.Type == RetaguardaClaims.Permission)
            .Select(claim => claim.Value)
            .ToHashSet(StringComparer.Ordinal);

        var added = 0;
        foreach (var permission in catalog.All.Where(p => !existing.Contains(p.Name)))
        {
            var result = await roleManager.AddClaimAsync(
                admin,
                new Claim(RetaguardaClaims.Permission, permission.Name));

            if (result.Succeeded)
            {
                added++;
            }
            else
            {
                logger.LogError(
                    "Falha ao conceder a permissão {Permission} ao papel {Role}: {Errors}",
                    permission.Name,
                    RetaguardaRoles.Admin,
                    string.Join("; ", result.Errors.Select(e => e.Description)));
            }
        }

        if (added > 0)
        {
            logger.LogInformation("Permissões concedidas ao papel {Role}: {Count}", RetaguardaRoles.Admin, added);
        }
    }
}
