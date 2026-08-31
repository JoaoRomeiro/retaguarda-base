using Retaguarda.Business.Users;
using Retaguarda.Data.Identity;

namespace Retaguarda.UnitTests.Users;

public sealed class SiteSelectionServiceTests
{
    private const string UserId = "u1";

    // Usuário associado às plantas 1 (ativa) e 2 (INATIVA). Planta 3 ativa, não associada.
    private static async Task<FakeUserRepository> BuildRepositoryAsync()
    {
        var repo = new FakeUserRepository();
        repo.SeedSite(1);
        repo.SeedSite(2, isActive: false);
        repo.SeedSite(3);

        var user = new ApplicationUser { Id = UserId, FullName = "Operador", Email = "op@x", DefaultSiteId = 1 };
        await repo.AddAsync(user, "Abcd@123", "Picker", [1]);
        repo.LinkSite(UserId, 2);
        return repo;
    }

    [Fact]
    public async Task GetSelectableSitesAsync_returns_only_active_linked_sites()
    {
        var repo = await BuildRepositoryAsync();
        var service = new SiteSelectionService(repo);

        var sites = await service.GetSelectableSitesAsync(UserId);

        // Só a planta 1 (a 2 é inativa; a 3 não está associada).
        Assert.Single(sites);
        Assert.Equal(1, sites[0].Id);
    }

    [Fact]
    public async Task IsSelectableAsync_true_for_active_linked_site()
    {
        var repo = await BuildRepositoryAsync();
        var service = new SiteSelectionService(repo);

        Assert.True(await service.IsSelectableAsync(UserId, 1));
    }

    [Fact]
    public async Task IsSelectableAsync_false_for_inactive_linked_site()
    {
        var repo = await BuildRepositoryAsync();
        var service = new SiteSelectionService(repo);

        Assert.False(await service.IsSelectableAsync(UserId, 2));
    }

    [Fact]
    public async Task IsSelectableAsync_false_for_unlinked_site()
    {
        var repo = await BuildRepositoryAsync();
        var service = new SiteSelectionService(repo);

        Assert.False(await service.IsSelectableAsync(UserId, 3));
    }
}
