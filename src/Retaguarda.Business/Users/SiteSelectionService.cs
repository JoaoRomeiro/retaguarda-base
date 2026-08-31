using Retaguarda.Business.Users.Dtos;
using Retaguarda.Data.Repositories;

namespace Retaguarda.Business.Users;

public sealed class SiteSelectionService : ISiteSelectionService
{
    private readonly IUserRepository _repository;

    public SiteSelectionService(IUserRepository repository) => _repository = repository;

    public async Task<IReadOnlyList<AvailableSiteDto>> GetSelectableSitesAsync(
        string userId, CancellationToken cancellationToken = default)
    {
        var sites = await _repository.GetActiveLinkedSitesAsync(userId, cancellationToken);
        return sites.Select(s => new AvailableSiteDto { Id = s.Id, Code = s.Code, Name = s.Name }).ToList();
    }

    public async Task<bool> IsSelectableAsync(string userId, int siteId, CancellationToken cancellationToken = default)
    {
        var sites = await _repository.GetActiveLinkedSitesAsync(userId, cancellationToken);
        return sites.Any(s => s.Id == siteId);
    }
}
