using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Retaguarda.Data.Entities;
using Retaguarda.Data.Identity;

namespace Retaguarda.Web.Infrastructure;

// Seed de DESENVOLVIMENTO: garante os papéis pré-cadastrados, uma planta e um usuário Admin
// inicial para entrar no sistema. Chamado apenas em Development.
// NUNCA executar em produção — as credenciais abaixo são fixas e públicas.
public static class DevelopmentDataSeeder
{
    public const string AdminEmail = "admin@retaguarda.local";
    private const string AdminPassword = "Admin@123";

    // Apenas o perfil Admin é pré-cadastrado; os papéis de cada projeto entram pelo CRUD de Papéis.
    private static readonly (string Name, string Description)[] SystemRoles =
    [
        ("Admin", "Acesso amplo ao sistema, incluindo cadastros e usuários"),
    ];

    public static async Task SeedAsync(IServiceProvider services)
    {
        var roleManager = services.GetRequiredService<RoleManager<ApplicationRole>>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var db = services.GetRequiredService<ApplicationDbContext>();
        var logger = services
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger(nameof(DevelopmentDataSeeder));

        // Garante os papéis internos (idempotente). Marca IsSystem e a descrição,
        // fazendo backfill em bancos que já tinham os papéis antes das colunas novas.
        foreach (var (name, description) in SystemRoles)
        {
            var role = await roleManager.FindByNameAsync(name);
            if (role is null)
            {
                await roleManager.CreateAsync(new ApplicationRole(name)
                {
                    Description = description,
                    IsSystem = true,
                });
            }
            else if (!role.IsSystem || role.Description != description)
            {
                role.IsSystem = true;
                role.Description = description;
                await roleManager.UpdateAsync(role);
            }
        }

        // Garante uma planta DEV: o admin precisa de uma planta padrão (DefaultSiteId obrigatório).
        var devSite = await db.Sites.FirstOrDefaultAsync();
        if (devSite is null)
        {
            devSite = new Site
            {
                Code = "DEV",
                Name = "Planta DEV",
                TimeZone = "America/Sao_Paulo",
                IsActive = true,
            };
            db.Sites.Add(devSite);
            await db.SaveChangesAsync();
            logger.LogInformation("Development seed site created: {Code}", devSite.Code);
        }

        // Garante o usuário admin (idempotente).
        var user = await userManager.FindByEmailAsync(AdminEmail);
        if (user is null)
        {
            user = new ApplicationUser
            {
                UserName = AdminEmail,
                Email = AdminEmail,
                EmailConfirmed = true,
                FullName = "Administrador",
                IsActive = true,
                DefaultSiteId = devSite.Id,
            };

            var result = await userManager.CreateAsync(user, AdminPassword);
            if (!result.Succeeded)
            {
                logger.LogWarning(
                    "Failed to create development seed user: {Errors}",
                    string.Join("; ", result.Errors.Select(e => e.Description)));
                return;
            }

            logger.LogInformation("Development seed user created: {Email}", AdminEmail);
        }
        else if (user.DefaultSiteId == 0 || string.IsNullOrEmpty(user.FullName))
        {
            // Backfill para bancos criados antes dos campos novos.
            user.FullName = string.IsNullOrEmpty(user.FullName) ? "Administrador" : user.FullName;
            user.IsActive = true;
            if (user.DefaultSiteId == 0)
            {
                user.DefaultSiteId = devSite.Id;
            }
            await userManager.UpdateAsync(user);
        }

        // Garante o vínculo de acesso do admin à planta DEV (N:N, idempotente).
        if (!await db.UserSites.AnyAsync(us => us.UserId == user.Id && us.SiteId == devSite.Id))
        {
            db.UserSites.Add(new UserSite { UserId = user.Id, SiteId = devSite.Id });
            await db.SaveChangesAsync();
            logger.LogInformation("Development seed user linked to site {Code}.", devSite.Code);
        }

        // Garante o vínculo com o papel Admin (idempotente — corrige bancos antigos).
        if (!await userManager.IsInRoleAsync(user, "Admin"))
        {
            await userManager.AddToRoleAsync(user, "Admin");
            logger.LogInformation("Development seed user added to Admin role: {Email}", AdminEmail);
        }
    }
}
