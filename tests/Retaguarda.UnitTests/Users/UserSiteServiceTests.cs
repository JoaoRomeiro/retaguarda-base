using FluentValidation;
using Retaguarda.Business.Users;
using Retaguarda.Business.Users.Dtos;
using Retaguarda.Business.Users.Validators;
using Retaguarda.Data.Identity;

namespace Retaguarda.UnitTests.Users;

public sealed class UserSiteServiceTests
{
    private const string UserId = "u1";

    // Cria o repositório com plantas 1..3 e um usuário com planta padrão = 1 (associada).
    private static async Task<FakeUserRepository> BuildRepositoryAsync()
    {
        var repo = new FakeUserRepository();
        repo.SeedSite(1);
        repo.SeedSite(2);
        repo.SeedSite(3);

        var user = new ApplicationUser { Id = UserId, FullName = "Operador", Email = "op@x", DefaultSiteId = 1 };
        await repo.AddAsync(user, "Abcd@123", "Picker", [1]);
        return repo;
    }

    private static UserSiteService BuildService(FakeUserRepository repository) =>
        new(repository, new AssociateSiteRequestValidator(repository));

    [Fact]
    public async Task AddAsync_links_available_site()
    {
        var repo = await BuildRepositoryAsync();
        var service = BuildService(repo);

        await service.AddAsync(new AssociateSiteRequest { UserId = UserId, SiteId = 2 });

        Assert.True(await repo.IsSiteLinkedAsync(UserId, 2));
    }

    [Fact]
    public async Task AddAsync_rejects_already_linked_site()
    {
        var repo = await BuildRepositoryAsync();
        var service = BuildService(repo);

        await Assert.ThrowsAsync<ValidationException>(
            () => service.AddAsync(new AssociateSiteRequest { UserId = UserId, SiteId = 1 }));
    }

    [Fact]
    public async Task AddAsync_rejects_unknown_site()
    {
        var repo = await BuildRepositoryAsync();
        var service = BuildService(repo);

        await Assert.ThrowsAsync<ValidationException>(
            () => service.AddAsync(new AssociateSiteRequest { UserId = UserId, SiteId = 99 }));
    }

    [Fact]
    public async Task AddAsync_rejects_inactive_site()
    {
        var repo = await BuildRepositoryAsync();
        repo.SeedSite(4, isActive: false);
        var service = BuildService(repo);

        await Assert.ThrowsAsync<ValidationException>(
            () => service.AddAsync(new AssociateSiteRequest { UserId = UserId, SiteId = 4 }));
    }

    [Fact]
    public async Task RemoveAsync_removes_non_default_site()
    {
        var repo = await BuildRepositoryAsync();
        var service = BuildService(repo);
        await service.AddAsync(new AssociateSiteRequest { UserId = UserId, SiteId = 2 });

        var result = await service.RemoveAsync(UserId, 2);

        Assert.Equal(SiteUnlinkResult.Removed, result);
        Assert.False(await repo.IsSiteLinkedAsync(UserId, 2));
    }

    [Fact]
    public async Task RemoveAsync_blocks_default_site()
    {
        var repo = await BuildRepositoryAsync();
        var service = BuildService(repo);

        var result = await service.RemoveAsync(UserId, 1);  // 1 é a planta padrão

        Assert.Equal(SiteUnlinkResult.IsDefault, result);
        Assert.True(await repo.IsSiteLinkedAsync(UserId, 1));
    }

    [Fact]
    public async Task RemoveAsync_returns_NotFound_when_site_not_linked()
    {
        var repo = await BuildRepositoryAsync();
        var service = BuildService(repo);

        var result = await service.RemoveAsync(UserId, 3);  // 3 não está associada

        Assert.Equal(SiteUnlinkResult.NotFound, result);
    }

    [Fact]
    public async Task ListAsync_marks_default_site()
    {
        var repo = await BuildRepositoryAsync();
        var service = BuildService(repo);
        await service.AddAsync(new AssociateSiteRequest { UserId = UserId, SiteId = 2 });

        var result = await service.ListAsync(UserId, search: null, page: 1, pageSize: 10);

        Assert.Equal(2, result.TotalCount);
        Assert.True(result.Items.Single(s => s.SiteId == 1).IsDefault);
        Assert.False(result.Items.Single(s => s.SiteId == 2).IsDefault);
    }
}
