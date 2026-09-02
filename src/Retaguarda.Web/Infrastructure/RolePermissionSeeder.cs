using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Retaguarda.Data.Identity;
using Retaguarda.Shared;
using Retaguarda.Shared.Authorization;

namespace Retaguarda.Web.Infrastructure;

/// <summary>
/// Reconcilia as permissões do papel interno Admin com o catálogo: concede o que falta e revoga o
/// que não existe mais.
///
/// Conceder não é conveniência, é trava de segurança: sem isso, alguém tira uma permissão do Admin e
/// ninguém mais consegue administrar nada — nem para desfazer. Permissão nova (inclusive as do
/// projeto derivado) já nasce concedida.
///
/// Revogar limpa a sobra de uma permissão que saiu do código: ela nunca mais casa com nada, mas
/// ficaria no banco para sempre, aparecendo em auditoria e em consulta como se ainda valesse.
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

        var known = catalog.All.Select(permission => permission.Name).ToHashSet(StringComparer.Ordinal);
        var removed = 0;
        foreach (var orphan in existing.Where(name => !known.Contains(name)))
        {
            var result = await roleManager.RemoveClaimAsync(
                admin,
                new Claim(RetaguardaClaims.Permission, orphan));

            if (result.Succeeded)
            {
                removed++;
            }
            else
            {
                logger.LogError(
                    "Falha ao revogar a permissão {Permission} do papel {Role}: {Errors}",
                    orphan,
                    RetaguardaRoles.Admin,
                    string.Join("; ", result.Errors.Select(e => e.Description)));
            }
        }

        if (added > 0 || removed > 0)
        {
            logger.LogInformation(
                "Permissões do papel {Role} reconciliadas: {Added} concedidas, {Removed} revogadas",
                RetaguardaRoles.Admin,
                added,
                removed);
        }
    }
}
