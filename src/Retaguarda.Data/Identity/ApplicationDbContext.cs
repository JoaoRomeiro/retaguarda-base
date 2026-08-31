using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Retaguarda.Data.Entities;
using Retaguarda.Shared.Contracts;

namespace Retaguarda.Data.Identity;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, string>
{
    // Usuário corrente: provê a planta ativa para o Global Query Filter multi-site.
    private readonly ICurrentUserService _currentUser;

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, ICurrentUserService currentUser)
        : base(options)
    {
        _currentUser = currentUser;
    }

    /// <summary>
    /// Planta ativa e usuário da requisição. Toda entidade do domínio é isolada por planta: ao
    /// criar a primeira, replique no <c>OnModelCreating</c> o filtro
    /// <c>entity.HasQueryFilter(x =&gt; !x.IsDeleted &amp;&amp; x.SiteId == CurrentUser.SiteId)</c>.
    /// </summary>
    protected ICurrentUserService CurrentUser => _currentUser;

    // Plantas — raiz do isolamento multi-site (não são filtradas por planta).
    public DbSet<Site> Sites => Set<Site>();

    // Vínculo N:N usuário↔planta (plantas que o usuário pode acessar).
    public DbSet<UserSite> UserSites => Set<UserSite>();

    // Refresh tokens da API. Infra de autenticação — não isolada por planta.
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Todas as tabelas do Identity vão para o schema "identity".
        // Usamos ToTable por entidade em vez de HasDefaultSchema para que as futuras
        // entidades de negócio fiquem em "dbo" (default) sem conflito.
        builder.Entity<IdentityUserRole<string>>().ToTable("UserRoles", "identity");
        builder.Entity<IdentityUserClaim<string>>().ToTable("UserClaims", "identity");
        builder.Entity<IdentityUserLogin<string>>().ToTable("UserLogins", "identity");
        builder.Entity<IdentityRoleClaim<string>>().ToTable("RoleClaims", "identity");
        builder.Entity<IdentityUserToken<string>>().ToTable("UserTokens", "identity");

        // Usuários (Identity) estendidos: nome, status, idioma, planta padrão, auditoria e
        // soft delete. Permanecem em [identity].[Users].
        builder.Entity<ApplicationUser>(entity =>
        {
            entity.ToTable("Users", "identity");

            entity.Property(u => u.FullName).IsRequired().HasMaxLength(200);
            entity.Property(u => u.PreferredLanguage).HasMaxLength(10);

            // Auditoria: *ById referenciam [identity].[Users].Id (sem FK formal, como no Site).
            entity.Property(u => u.CreatedById).HasMaxLength(450);
            entity.Property(u => u.UpdatedById).HasMaxLength(450);
            entity.Property(u => u.DeletedById).HasMaxLength(450);

            // Planta padrão: FK sem cascade (sites são excluídos logicamente, não fisicamente).
            entity.HasOne(u => u.DefaultSite)
                .WithMany()
                .HasForeignKey(u => u.DefaultSiteId)
                .OnDelete(DeleteBehavior.Restrict);

            // Soft delete transversal: usuários excluídos somem das queries (inclusive de login).
            entity.HasQueryFilter(u => !u.IsDeleted);
        });

        // Vínculo N:N usuário↔planta. PK composta; remover o usuário remove os vínculos,
        // mas a planta é protegida (Restrict).
        builder.Entity<UserSite>(entity =>
        {
            entity.ToTable("UserSites", "identity");
            entity.HasKey(us => new { us.UserId, us.SiteId });

            entity.HasOne(us => us.User)
                .WithMany(u => u.SiteLinks)
                .HasForeignKey(us => us.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(us => us.Site)
                .WithMany()
                .HasForeignKey(us => us.SiteId)
                .OnDelete(DeleteBehavior.Restrict);

            // Filtro "casado" com os principais (User e Site têm filtro !IsDeleted): vínculos de
            // usuários ou plantas soft-deletados somem, consistente com o soft delete transversal.
            entity.HasQueryFilter(us => !us.User.IsDeleted && !us.Site.IsDeleted);
        });

        // Papéis (Identity) estendidos: descrição, IsSystem, auditoria e soft delete.
        // Permanecem em [identity].[Roles].
        builder.Entity<ApplicationRole>(entity =>
        {
            entity.ToTable("Roles", "identity");

            entity.Property(r => r.Description).HasMaxLength(200);

            // Auditoria: *ById referenciam [identity].[Users].Id (sem FK formal, como no Site).
            entity.Property(r => r.CreatedById).HasMaxLength(450);
            entity.Property(r => r.UpdatedById).HasMaxLength(450);
            entity.Property(r => r.DeletedById).HasMaxLength(450);

            // Soft delete transversal: papéis excluídos somem das queries (inclusive do RoleManager).
            entity.HasQueryFilter(r => !r.IsDeleted);

            // RoleNameIndex (único em NormalizedName, criado pelo Identity) passa a ser filtrado
            // por IsDeleted = 0: permite reutilizar o nome de um papel excluído (como o Site.Code).
            entity.HasIndex(r => r.NormalizedName)
                .HasDatabaseName("RoleNameIndex")
                .IsUnique()
                .HasFilter("\"IsDeleted\" = false");
        });

        builder.Entity<Site>(entity =>
        {
            entity.ToTable("Sites");  // schema dbo (default)

            entity.Property(s => s.Name).IsRequired().HasMaxLength(200);
            entity.Property(s => s.Code).IsRequired().HasMaxLength(20);
            entity.Property(s => s.TimeZone).IsRequired().HasMaxLength(50);

            // Auditoria: *ById referenciam AspNetUsers.Id (sem FK formal para evitar
            // múltiplos caminhos de cascade até Users).
            entity.Property(s => s.CreatedById).HasMaxLength(450);
            entity.Property(s => s.UpdatedById).HasMaxLength(450);
            entity.Property(s => s.DeletedById).HasMaxLength(450);

            // Code é único apenas entre plantas ATIVAS (filtrado por IsDeleted = 0): permite
            // reaproveitar o código de uma planta excluída logicamente.
            entity.HasIndex(s => s.Code).IsUnique().HasFilter("\"IsDeleted\" = false");

            // Name também é único entre plantas ATIVAS (mesma estratégia do Code).
            entity.HasIndex(s => s.Name).IsUnique().HasFilter("\"IsDeleted\" = false");

            // Soft delete transversal: registros excluídos somem das queries por padrão.
            entity.HasQueryFilter(s => !s.IsDeleted);
        });

        // Refresh tokens (schema [identity], junto dos demais artefatos de autenticação).
        // SEM Global Query Filter por planta: são consultados na renovação/logout, quando
        // ainda não há planta ativa no contexto. O SiteId é apenas o dado da sessão fixada.
        builder.Entity<RefreshToken>(entity =>
        {
            entity.ToTable("RefreshTokens", "identity");

            entity.Property(t => t.TokenHash).IsRequired().HasMaxLength(128);
            entity.Property(t => t.UserId).IsRequired().HasMaxLength(450);

            // Dono do token: remover o usuário remove seus refresh tokens (cascade).
            entity.HasOne(t => t.User)
                .WithMany()
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Hash único: o token é localizado por hash na renovação e no logout.
            entity.HasIndex(t => t.TokenHash).IsUnique();
            entity.HasIndex(t => t.UserId);

            // Filtro "casado" com o principal (ApplicationUser tem filtro !IsDeleted): tokens de
            // usuários soft-deletados somem — o fluxo de auth já revalida IsActive/planta.
            entity.HasQueryFilter(t => !t.User!.IsDeleted);
        });
    }
}
