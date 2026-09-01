using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Retaguarda.Data.Entities;
using Retaguarda.Shared;
using Retaguarda.Data.Identity;

namespace Retaguarda.Web.Infrastructure;

/// <summary>
/// Bootstrap de PRODUÇÃO: garante o papel Admin, uma planta inicial e o usuário admin a partir da
/// configuração (<c>Seed:AdminEmail</c> / <c>Seed:AdminPassword</c>, vindos do <c>.env</c>).
/// Idempotente: só cria o que falta e NUNCA redefine a senha de um usuário já existente (uma troca
/// de senha pelo app não é revertida em restart). Se as credenciais não estiverem configuradas,
/// apenas loga e não faz nada. Ver docs/deploy.md.
/// </summary>
public static class ProductionDataSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        var config = services.GetRequiredService<IConfiguration>();
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(ProductionDataSeeder));

        var email = config["Seed:AdminEmail"];
        var password = config["Seed:AdminPassword"];
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            logger.LogWarning(
                "Seed:AdminEmail/Seed:AdminPassword não configurados — admin de produção não criado.");
            return;
        }

        var roleManager = services.GetRequiredService<RoleManager<ApplicationRole>>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var db = services.GetRequiredService<ApplicationDbContext>();

        // Papel Admin (interno).
        if (await roleManager.FindByNameAsync(RetaguardaRoles.Admin) is null)
        {
            await roleManager.CreateAsync(new ApplicationRole(RetaguardaRoles.Admin)
            {
                Description = "Acesso amplo ao sistema, incluindo cadastros e usuários",
                IsSystem = true,
            });
        }

        // Planta inicial: o login exige uma planta ativa. Só cria se não houver nenhuma (o admin pode
        // renomear/criar plantas depois no cadastro).
        var site = await db.Sites.FirstOrDefaultAsync();
        if (site is null)
        {
            site = new Site { Code = "MATRIZ", Name = "Matriz", TimeZone = "America/Sao_Paulo", IsActive = true };
            db.Sites.Add(site);
            await db.SaveChangesAsync();
            logger.LogInformation("Planta inicial criada: {Code}", site.Code);
        }

        // Usuário admin (idempotente — não mexe na senha se já existir).
        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                FullName = "Administrador",
                IsActive = true,
                DefaultSiteId = site.Id,
            };

            var result = await userManager.CreateAsync(user, password);
            if (!result.Succeeded)
            {
                logger.LogError(
                    "Falha ao criar o admin de produção: {Errors}",
                    string.Join("; ", result.Errors.Select(e => e.Description)));
                return;
            }

            // Sem o e-mail: é dado pessoal e o log fica 30 dias em arquivo (LGPD). O UserId basta
            // para correlacionar; quem operou o deploy já conhece o e-mail (veio do .env).
            logger.LogInformation("Usuário admin de produção criado: {UserId}", user.Id);
        }

        // Vínculo com a planta (N:N) e o papel Admin (idempotentes).
        if (!await db.UserSites.AnyAsync(us => us.UserId == user.Id && us.SiteId == site.Id))
        {
            db.UserSites.Add(new UserSite { UserId = user.Id, SiteId = site.Id });
            await db.SaveChangesAsync();
        }

        if (!await userManager.IsInRoleAsync(user, RetaguardaRoles.Admin))
        {
            await userManager.AddToRoleAsync(user, RetaguardaRoles.Admin);
        }
    }
}
