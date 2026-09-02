using Retaguarda.Shared.Authorization;

namespace Retaguarda.UnitTests.Authorization;

public sealed class PermissionCatalogTests
{
    private sealed class StubProvider(params PermissionDefinition[] permissions) : IPermissionProvider
    {
        public IEnumerable<PermissionDefinition> GetPermissions() => permissions;
    }

    private static PermissionCatalog Build(params IPermissionProvider[] providers) => new(providers);

    [Fact]
    public void Platform_permissions_are_all_in_the_catalog()
    {
        var catalog = Build(new PlatformPermissions.Provider());

        Assert.True(catalog.Contains(PlatformPermissions.Sites.Edit));
        Assert.True(catalog.Contains(PlatformPermissions.UserSites.View));
        Assert.True(catalog.Contains(PlatformPermissions.Roles.Delete));
        Assert.Equal(16, catalog.All.Count);
    }

    [Fact]
    public void Catalog_merges_permissions_from_every_provider()
    {
        // É assim que o projeto derivado acrescenta o domínio dele sem tocar na base.
        var catalog = Build(
            new PlatformPermissions.Provider(),
            new StubProvider(new PermissionDefinition("orders.view", "orders")));

        Assert.True(catalog.Contains("orders.view"));
        Assert.True(catalog.Contains(PlatformPermissions.Sites.View));
    }

    [Fact]
    public void Unknown_permission_is_not_in_the_catalog()
    {
        var catalog = Build(new PlatformPermissions.Provider());

        Assert.False(catalog.Contains("sites.edt"));
        Assert.False(catalog.Contains(""));
    }

    [Theory]
    [InlineData("Sites.Edit")]     // maiúscula
    [InlineData("sites")]          // sem ação
    [InlineData("sites.")]         // ação vazia
    [InlineData("sites edit")]     // espaço
    public void Invalid_name_fails_on_startup(string name)
    {
        var provider = new StubProvider(new PermissionDefinition(name, "sites"));

        Assert.Throws<InvalidOperationException>(() => Build(provider));
    }

    [Fact]
    public void Name_must_belong_to_the_declared_resource()
    {
        var provider = new StubProvider(new PermissionDefinition("users.edit", "sites"));

        Assert.Throws<InvalidOperationException>(() => Build(provider));
    }

    [Fact]
    public void Duplicated_permission_fails_on_startup()
    {
        var duplicate = new StubProvider(new PermissionDefinition(PlatformPermissions.Sites.View, "sites"));

        Assert.Throws<InvalidOperationException>(() => Build(new PlatformPermissions.Provider(), duplicate));
    }

    [Fact]
    public void Grouping_by_resource_keeps_every_permission()
    {
        var catalog = Build(new PlatformPermissions.Provider());

        Assert.Equal(4, catalog.ByResource.Count);
        Assert.Equal(catalog.All.Count, catalog.ByResource.Sum(group => group.Count()));
    }

    [Fact]
    public void Resource_key_is_derived_from_the_name()
    {
        var permission = new PermissionDefinition(PlatformPermissions.UserSites.Delete, "usersites");

        Assert.Equal("permission_usersites_delete", permission.ResourceKey);
    }
}
