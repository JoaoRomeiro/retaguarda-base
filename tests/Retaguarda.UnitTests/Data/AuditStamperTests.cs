using Retaguarda.Data.Entities;
using Retaguarda.Data.Interceptors;
using Retaguarda.Shared.Contracts;

namespace Retaguarda.UnitTests.Data;

// Valida a lógica pura de carimbo de auditoria/soft delete (sem EF Core).
public sealed class AuditStamperTests
{
    private static readonly DateTime UtcNow = new(2026, 5, 25, 12, 0, 0, DateTimeKind.Utc);
    private const string UserId = "user-123";

    [Fact]
    public void StampCreated_sets_created_fields()
    {
        var site = new Site();

        AuditStamper.StampCreated(site, UserId, UtcNow);

        Assert.Equal(UtcNow, site.CreatedAt);
        Assert.Equal(UserId, site.CreatedById);
        Assert.Null(site.UpdatedAt);
        Assert.Null(site.UpdatedById);
        Assert.False(site.IsDeleted);
    }

    [Fact]
    public void StampUpdated_sets_updated_fields()
    {
        var site = new Site();

        AuditStamper.StampUpdated(site, UserId, UtcNow);

        Assert.Equal(UtcNow, site.UpdatedAt);
        Assert.Equal(UserId, site.UpdatedById);
    }

    [Fact]
    public void StampDeleted_marks_as_deleted_without_removing()
    {
        var site = new Site();

        AuditStamper.StampDeleted(site, UserId, UtcNow);

        Assert.True(site.IsDeleted);
        Assert.Equal(UtcNow, site.DeletedAt);
        Assert.Equal(UserId, site.DeletedById);
    }

    [Fact]
    public void StampCreated_accepts_anonymous_user()
    {
        var site = new Site();

        AuditStamper.StampCreated(site, userId: null, UtcNow);

        Assert.Equal(UtcNow, site.CreatedAt);
        Assert.Null(site.CreatedById);
    }

    [Fact]
    public void StampSite_sets_active_site_when_unset()
    {
        var scoped = new FakeSiteScoped();

        AuditStamper.StampSite(scoped, siteId: 7);

        Assert.Equal(7, scoped.SiteId);
    }

    [Fact]
    public void StampSite_does_not_overwrite_explicit_site()
    {
        var scoped = new FakeSiteScoped { SiteId = 3 };

        AuditStamper.StampSite(scoped, siteId: 7);

        Assert.Equal(3, scoped.SiteId);
    }

    [Fact]
    public void StampSite_leaves_unset_when_no_active_site()
    {
        var scoped = new FakeSiteScoped();

        AuditStamper.StampSite(scoped, siteId: null);

        Assert.Equal(0, scoped.SiteId);
    }

    // Duble mínimo de uma entidade multi-site: exercita StampSite sem depender de nenhuma
    // entidade de domínio (as entidades ISiteScoped concretas nascem em cada projeto).
    private sealed class FakeSiteScoped : ISiteScoped
    {
        public int SiteId { get; set; }
    }
}
