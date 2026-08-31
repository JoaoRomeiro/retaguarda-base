using FluentValidation;
using Retaguarda.Business.Roles;
using Retaguarda.Business.Roles.Dtos;
using Retaguarda.Business.Roles.Validators;
using Retaguarda.Data.Identity;

namespace Retaguarda.UnitTests.Roles;

public sealed class RoleServiceTests
{
    private static RoleService BuildService(FakeRoleRepository repository) =>
        new(repository, new CreateRoleRequestValidator(repository), new UpdateRoleRequestValidator(repository));

    private static CreateRoleRequest ValidCreate(string name = "Operators") => new()
    {
        Name = name,
        Description = "Operadores de chão de fábrica",
    };

    [Fact]
    public async Task CreateAsync_persists_and_returns_dto()
    {
        var repository = new FakeRoleRepository();
        var service = BuildService(repository);

        var dto = await service.CreateAsync(ValidCreate());

        Assert.False(string.IsNullOrEmpty(dto.Id));
        Assert.Equal("Operators", dto.Name);
        Assert.False(dto.IsSystem);
        Assert.Single(repository.Store);
    }

    [Fact]
    public async Task CreateAsync_rejects_duplicate_name()
    {
        var repository = new FakeRoleRepository();
        var service = BuildService(repository);
        await service.CreateAsync(ValidCreate("Operators"));

        await Assert.ThrowsAsync<ValidationException>(
            () => service.CreateAsync(ValidCreate("Operators")));
    }

    [Fact]
    public async Task CreateAsync_rejects_empty_name()
    {
        var repository = new FakeRoleRepository();
        var service = BuildService(repository);

        var request = ValidCreate();
        request.Name = "";

        await Assert.ThrowsAsync<ValidationException>(() => service.CreateAsync(request));
    }

    [Fact]
    public async Task CreateAsync_rejects_too_long_name()
    {
        var repository = new FakeRoleRepository();
        var service = BuildService(repository);

        var request = ValidCreate();
        request.Name = new string('A', 257);  // limite é 256

        await Assert.ThrowsAsync<ValidationException>(() => service.CreateAsync(request));
    }

    [Fact]
    public async Task CreateAsync_rejects_too_long_description()
    {
        var repository = new FakeRoleRepository();
        var service = BuildService(repository);

        var request = ValidCreate();
        request.Description = new string('A', 201);  // limite é 200

        await Assert.ThrowsAsync<ValidationException>(() => service.CreateAsync(request));
    }

    [Fact]
    public async Task UpdateAsync_returns_false_when_role_missing()
    {
        var repository = new FakeRoleRepository();
        var service = BuildService(repository);

        var ok = await service.UpdateAsync(new UpdateRoleRequest
        {
            Id = "missing",
            Name = "Whatever",
        });

        Assert.False(ok);
    }

    [Fact]
    public async Task UpdateAsync_keeps_same_name_for_same_role()
    {
        var repository = new FakeRoleRepository();
        var service = BuildService(repository);
        var created = await service.CreateAsync(ValidCreate("Operators"));

        var ok = await service.UpdateAsync(new UpdateRoleRequest
        {
            Id = created.Id,
            Name = "Operators",  // mesmo nome do próprio papel não deve falhar
            Description = "Nova descrição",
        });

        Assert.True(ok);
        Assert.Equal("Nova descrição", repository.Store[0].Description);
    }

    [Fact]
    public async Task UpdateAsync_system_role_name_is_immutable()
    {
        var repository = new FakeRoleRepository();
        var service = BuildService(repository);
        var admin = repository.Seed(new ApplicationRole("Admin")
        {
            Id = "admin-id",
            Description = "Acesso amplo",
            IsSystem = true,
        });

        var ok = await service.UpdateAsync(new UpdateRoleRequest
        {
            Id = admin.Id,
            Name = "SuperAdmin",       // tentativa de renomear papel interno
            Description = "Atualizada",
        });

        Assert.True(ok);
        Assert.Equal("Admin", repository.Store[0].Name);          // nome preservado
        Assert.Equal("Atualizada", repository.Store[0].Description); // descrição alterada
    }

    [Fact]
    public async Task DeleteAsync_soft_deletes_custom_role()
    {
        var repository = new FakeRoleRepository();
        var service = BuildService(repository);
        var created = await service.CreateAsync(ValidCreate("Operators"));

        var result = await service.DeleteAsync(created.Id);

        Assert.Equal(RoleDeletionResult.Deleted, result);
        Assert.True(repository.Store[0].IsDeleted);
    }

    [Fact]
    public async Task DeleteAsync_returns_NotFound_when_missing()
    {
        var repository = new FakeRoleRepository();
        var service = BuildService(repository);

        Assert.Equal(RoleDeletionResult.NotFound, await service.DeleteAsync("missing"));
    }

    [Fact]
    public async Task DeleteAsync_blocks_system_role()
    {
        var repository = new FakeRoleRepository();
        var service = BuildService(repository);
        var admin = repository.Seed(new ApplicationRole("Admin")
        {
            Id = "admin-id",
            IsSystem = true,
        });

        var result = await service.DeleteAsync(admin.Id);

        Assert.Equal(RoleDeletionResult.SystemRole, result);
        Assert.False(repository.Store[0].IsDeleted);
    }

    [Fact]
    public async Task DeleteAsync_blocks_role_with_assigned_users()
    {
        var repository = new FakeRoleRepository();
        var service = BuildService(repository);
        var created = await service.CreateAsync(ValidCreate("Operators"));
        repository.SetUsersInRole(created.Id, 3);

        var result = await service.DeleteAsync(created.Id);

        Assert.Equal(RoleDeletionResult.HasAssignedUsers, result);
        Assert.False(repository.Store[0].IsDeleted);
    }

    [Fact]
    public async Task CreateAsync_allows_reusing_name_of_deleted_role()
    {
        var repository = new FakeRoleRepository();
        var service = BuildService(repository);
        var created = await service.CreateAsync(ValidCreate("Temp"));
        await service.DeleteAsync(created.Id);

        // Recadastrar o mesmo nome após exclusão lógica deve funcionar.
        var recreated = await service.CreateAsync(ValidCreate("Temp"));

        Assert.False(string.IsNullOrEmpty(recreated.Id));
        Assert.NotEqual(created.Id, recreated.Id);
    }
}
