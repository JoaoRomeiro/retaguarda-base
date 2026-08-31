using FluentValidation;
using Retaguarda.Business.Users;
using Retaguarda.Business.Users.Dtos;
using Retaguarda.Business.Users.Validators;

namespace Retaguarda.UnitTests.Users;

public sealed class UserServiceTests
{
    private static FakeUserRepository BuildRepository()
    {
        var repo = new FakeUserRepository();
        repo.SeedRole("Picker");
        repo.SeedRole("Manager");
        repo.SeedSite(1);
        repo.SeedSite(2);
        return repo;
    }

    private static UserService BuildService(FakeUserRepository repository) =>
        new(repository, new CreateUserRequestValidator(repository), new UpdateUserRequestValidator(repository));

    private static CreateUserRequest ValidCreate(string email = "op@tibrasil.local") => new()
    {
        FullName = "Operador",
        Email = email,
        Password = "Abcd@123",
        RoleName = "Picker",
        DefaultSiteId = 1,
        IsActive = true,
    };

    [Fact]
    public async Task CreateAsync_persists_and_returns_dto()
    {
        var repo = BuildRepository();
        var service = BuildService(repo);

        var dto = await service.CreateAsync(ValidCreate());

        Assert.False(string.IsNullOrEmpty(dto.Id));
        Assert.Equal("op@tibrasil.local", dto.Email);
        Assert.Equal("Picker", dto.RoleName);
        Assert.Single(repo.Users);
    }

    [Fact]
    public async Task CreateAsync_associates_chosen_site_as_default()
    {
        var repo = BuildRepository();
        var service = BuildService(repo);

        var dto = await service.CreateAsync(ValidCreate());

        // A planta escolhida é a padrão e a única associação inicial.
        Assert.Equal(1, dto.DefaultSiteId);
        Assert.Equal([1], await repo.GetSiteIdsAsync(dto.Id));
        Assert.Equal("Picker", await repo.GetRoleNameAsync(dto.Id));
    }

    [Fact]
    public async Task CreateAsync_rejects_duplicate_email()
    {
        var repo = BuildRepository();
        var service = BuildService(repo);
        await service.CreateAsync(ValidCreate("dup@tibrasil.local"));

        await Assert.ThrowsAsync<ValidationException>(
            () => service.CreateAsync(ValidCreate("dup@tibrasil.local")));
    }

    [Fact]
    public async Task CreateAsync_rejects_invalid_email()
    {
        var repo = BuildRepository();
        var service = BuildService(repo);

        var request = ValidCreate();
        request.Email = "not-an-email";

        await Assert.ThrowsAsync<ValidationException>(() => service.CreateAsync(request));
    }

    [Fact]
    public async Task CreateAsync_rejects_weak_password()
    {
        var repo = BuildRepository();
        var service = BuildService(repo);

        var request = ValidCreate();
        request.Password = "abc";

        await Assert.ThrowsAsync<ValidationException>(() => service.CreateAsync(request));
    }

    [Fact]
    public async Task CreateAsync_rejects_unknown_role()
    {
        var repo = BuildRepository();
        var service = BuildService(repo);

        var request = ValidCreate();
        request.RoleName = "Ghost";

        await Assert.ThrowsAsync<ValidationException>(() => service.CreateAsync(request));
    }

    [Fact]
    public async Task CreateAsync_rejects_unknown_site()
    {
        var repo = BuildRepository();
        var service = BuildService(repo);

        var request = ValidCreate();
        request.DefaultSiteId = 99;

        await Assert.ThrowsAsync<ValidationException>(() => service.CreateAsync(request));
    }

    [Fact]
    public async Task UpdateAsync_returns_false_when_user_missing()
    {
        var repo = BuildRepository();
        var service = BuildService(repo);

        var ok = await service.UpdateAsync(new UpdateUserRequest
        {
            Id = "missing",
            FullName = "X",
            RoleName = "Picker",
            DefaultSiteId = 1,
        });

        Assert.False(ok);
    }

    [Fact]
    public async Task UpdateAsync_changes_role_and_default_among_linked_sites()
    {
        var repo = BuildRepository();
        var service = BuildService(repo);
        var created = await service.CreateAsync(ValidCreate());
        repo.LinkSite(created.Id, 2);  // planta 2 passa a estar associada

        var ok = await service.UpdateAsync(new UpdateUserRequest
        {
            Id = created.Id,
            FullName = "Operador Sr.",
            RoleName = "Manager",
            DefaultSiteId = 2,
            IsActive = true,
        });

        Assert.True(ok);
        Assert.Equal("Manager", await repo.GetRoleNameAsync(created.Id));
        Assert.Equal(2, repo.Users[0].DefaultSiteId);
    }

    [Fact]
    public async Task UpdateAsync_rejects_default_site_not_linked()
    {
        var repo = BuildRepository();
        var service = BuildService(repo);
        var created = await service.CreateAsync(ValidCreate());  // associado só à planta 1

        await Assert.ThrowsAsync<ValidationException>(() => service.UpdateAsync(new UpdateUserRequest
        {
            Id = created.Id,
            FullName = "Operador",
            RoleName = "Picker",
            DefaultSiteId = 2,  // não associada
            IsActive = true,
        }));
    }

    [Fact]
    public async Task DeleteAsync_blocks_self_delete()
    {
        var repo = BuildRepository();
        var service = BuildService(repo);
        var created = await service.CreateAsync(ValidCreate());

        var result = await service.DeleteAsync(created.Id, currentUserId: created.Id);

        Assert.Equal(UserDeletionResult.SelfDelete, result);
        Assert.False(repo.Users[0].IsDeleted);
    }

    [Fact]
    public async Task DeleteAsync_returns_NotFound_when_missing()
    {
        var repo = BuildRepository();
        var service = BuildService(repo);

        Assert.Equal(
            UserDeletionResult.NotFound,
            await service.DeleteAsync("missing", currentUserId: "admin"));
    }

    [Fact]
    public async Task DeleteAsync_soft_deletes_other_user()
    {
        var repo = BuildRepository();
        var service = BuildService(repo);
        var created = await service.CreateAsync(ValidCreate());

        var result = await service.DeleteAsync(created.Id, currentUserId: "admin");

        Assert.Equal(UserDeletionResult.Deleted, result);
        Assert.True(repo.Users[0].IsDeleted);
    }
}
