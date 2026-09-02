using FluentValidation;
using Retaguarda.Business.Roles;
using Retaguarda.Business.Roles.Dtos;
using Retaguarda.Business.Roles.Validators;
using Retaguarda.Data.Identity;
using Retaguarda.Shared.Authorization;

namespace Retaguarda.UnitTests.Roles;

/// <summary>
/// Concessão de permissões pelo cadastro de Acessos (etapa 2 da autorização por permissão).
/// </summary>
public sealed class RolePermissionsTests
{
    private static readonly PermissionCatalog Catalog = new([new PlatformPermissions.Provider()]);

    private static RoleService BuildService(FakeRoleRepository repository) =>
        new(repository,
            new CreateRoleRequestValidator(repository, Catalog),
            new UpdateRoleRequestValidator(repository, Catalog));

    [Fact]
    public async Task CreateAsync_grants_the_selected_permissions()
    {
        var repository = new FakeRoleRepository();
        var service = BuildService(repository);

        var dto = await service.CreateAsync(new CreateRoleRequest
        {
            Name = "Operators",
            Permissions = [PlatformPermissions.Sites.View, PlatformPermissions.Users.View],
        });

        var stored = await repository.GetPermissionsAsync(dto.Id);
        Assert.Equal([PlatformPermissions.Sites.View, PlatformPermissions.Users.View], stored);
        Assert.Equal(stored, dto.Permissions);
    }

    [Fact]
    public async Task CreateAsync_accepts_a_role_without_any_permission()
    {
        var repository = new FakeRoleRepository();
        var service = BuildService(repository);

        var dto = await service.CreateAsync(new CreateRoleRequest { Name = "Empty" });

        Assert.Empty(await repository.GetPermissionsAsync(dto.Id));
    }

    [Fact]
    public async Task CreateAsync_rejects_a_permission_outside_the_catalog()
    {
        var repository = new FakeRoleRepository();
        var service = BuildService(repository);

        var request = new CreateRoleRequest { Name = "Operators", Permissions = ["sites.edt"] };

        var exception = await Assert.ThrowsAsync<ValidationException>(() => service.CreateAsync(request));
        Assert.Contains(exception.Errors, error => error.ErrorMessage == "role_permission_unknown");
    }

    [Fact]
    public async Task CreateAsync_drops_duplicates_and_blank_entries()
    {
        var repository = new FakeRoleRepository();
        var service = BuildService(repository);

        var dto = await service.CreateAsync(new CreateRoleRequest
        {
            Name = "Operators",
            Permissions = [PlatformPermissions.Sites.View, PlatformPermissions.Sites.View, "  "],
        });

        Assert.Equal([PlatformPermissions.Sites.View], await repository.GetPermissionsAsync(dto.Id));
    }

    [Fact]
    public async Task UpdateAsync_replaces_the_permission_set()
    {
        var repository = new FakeRoleRepository();
        var role = repository.Seed(new ApplicationRole("Operators"));
        repository.SeedPermissions(role.Id, PlatformPermissions.Sites.View, PlatformPermissions.Sites.Edit);
        var service = BuildService(repository);

        var updated = await service.UpdateAsync(new UpdateRoleRequest
        {
            Id = role.Id,
            Name = "Operators",
            Permissions = [PlatformPermissions.Users.View],
        });

        Assert.True(updated);
        Assert.Equal([PlatformPermissions.Users.View], await repository.GetPermissionsAsync(role.Id));
    }

    [Fact]
    public async Task UpdateAsync_can_revoke_every_permission_of_a_custom_role()
    {
        var repository = new FakeRoleRepository();
        var role = repository.Seed(new ApplicationRole("Operators"));
        repository.SeedPermissions(role.Id, PlatformPermissions.Sites.View);
        var service = BuildService(repository);

        await service.UpdateAsync(new UpdateRoleRequest { Id = role.Id, Name = "Operators" });

        Assert.Empty(await repository.GetPermissionsAsync(role.Id));
    }

    [Fact]
    public async Task UpdateAsync_never_changes_the_permissions_of_a_system_role()
    {
        // A trava que impede trancar o administrador para fora: um POST forjado com a lista vazia
        // não pode revogar nada do papel interno.
        var repository = new FakeRoleRepository();
        var admin = repository.Seed(new ApplicationRole("Admin") { IsSystem = true });
        repository.SeedPermissions(admin.Id, [.. Catalog.All.Select(p => p.Name)]);
        var service = BuildService(repository);

        await service.UpdateAsync(new UpdateRoleRequest { Id = admin.Id, Name = "Admin", Permissions = [] });

        Assert.Equal(Catalog.All.Count, (await repository.GetPermissionsAsync(admin.Id)).Count);
    }

    [Fact]
    public async Task GetByIdAsync_returns_the_granted_permissions()
    {
        var repository = new FakeRoleRepository();
        var role = repository.Seed(new ApplicationRole("Operators"));
        repository.SeedPermissions(role.Id, PlatformPermissions.Roles.View);
        var service = BuildService(repository);

        var dto = await service.GetByIdAsync(role.Id);

        Assert.NotNull(dto);
        Assert.Equal([PlatformPermissions.Roles.View], dto.Permissions);
    }

    [Fact]
    public void Every_catalog_permission_has_a_label_and_a_group_header_in_the_resx()
    {
        // Permissão sem rótulo apareceria na tela como a própria chave (ex.: permission_sites_edit).
        var resx = File.ReadAllText(ResxPath());

        var missing = Catalog.All
            .Select(permission => permission.ResourceKey)
            .Concat(Catalog.ByResource.Select(group => $"permission_resource_{group.Key}"))
            .Where(key => !resx.Contains($"name=\"{key}\"", StringComparison.Ordinal))
            .ToList();

        Assert.True(missing.Count == 0, "Sem rótulo no .resx: " + string.Join(", ", missing));
    }

    private static string ResxPath()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Retaguarda.sln")))
        {
            directory = directory.Parent;
        }

        Assert.NotNull(directory);
        return Path.Combine(directory.FullName, "src", "Retaguarda.Shared", "Resources", "SharedResources.pt-BR.resx");
    }
}
