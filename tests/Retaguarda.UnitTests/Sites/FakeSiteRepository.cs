using Retaguarda.Data.Entities;
using Retaguarda.Data.Repositories;

namespace Retaguarda.UnitTests.Sites;

// Repositório em memória para testar SiteService/validators sem provider EF nem lib de mock.
internal sealed class FakeSiteRepository : ISiteRepository
{
    private readonly List<Site> _store = [];
    private int _nextId = 1;

    public IReadOnlyList<Site> Store => _store;

    public Task<Site?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        => Task.FromResult(_store.FirstOrDefault(s => s.Id == id && !s.IsDeleted));

    public Task<(IReadOnlyList<Site> Items, int TotalCount)> ListAsync(
        string? search, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        IEnumerable<Site> query = _store.Where(s => !s.IsDeleted);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(s =>
                s.Name.Contains(term, StringComparison.OrdinalIgnoreCase)
                || s.Code.Contains(term, StringComparison.OrdinalIgnoreCase));
        }

        var ordered = query.OrderBy(s => s.Name).ToList();
        var items = ordered.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        return Task.FromResult(((IReadOnlyList<Site>)items, ordered.Count));
    }

    public Task<bool> CodeExistsAsync(string code, int? excludeId, CancellationToken cancellationToken = default)
        => Task.FromResult(_store.Any(s =>
            s.Code == code && !s.IsDeleted && (excludeId == null || s.Id != excludeId)));

    public Task<bool> NameExistsAsync(string name, int? excludeId, CancellationToken cancellationToken = default)
        => Task.FromResult(_store.Any(s =>
            s.Name == name && !s.IsDeleted && (excludeId == null || s.Id != excludeId)));

    public Task<Site> AddAsync(Site site, CancellationToken cancellationToken = default)
    {
        site.Id = _nextId++;
        _store.Add(site);
        return Task.FromResult(site);
    }

    public Task UpdateAsync(Site site, CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task DeleteAsync(Site site, CancellationToken cancellationToken = default)
    {
        site.IsDeleted = true;
        return Task.CompletedTask;
    }
}
