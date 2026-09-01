using Retaguarda.Data.Entities;
using Retaguarda.Data.Identity;
using Retaguarda.Data.Repositories;

namespace Retaguarda.UnitTests.Users;

// Repositório em memória para testar UserService/validators sem UserManager/EF nem mocks.
internal sealed class FakeUserRepository : IUserRepository
{
    private readonly List<ApplicationUser> _users = [];
    private readonly Dictionary<string, string?> _roleByUser = [];
    private readonly Dictionary<string, HashSet<int>> _sitesByUser = [];
    private readonly HashSet<string> _roles = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<int, Site> _sites = [];

    public IReadOnlyList<ApplicationUser> Users => _users;

    public void SeedRole(string name) => _roles.Add(name);

    public void SeedSite(int id, bool isActive = true) =>
        _sites[id] = new Site { Id = id, Code = $"S{id:00}", Name = $"Planta {id}", IsActive = isActive };

    // Vincula uma planta a um usuário (para montar cenários de multi-planta nos testes).
    public void LinkSite(string userId, int siteId)
    {
        if (!_sitesByUser.TryGetValue(userId, out var set))
        {
            set = [];
            _sitesByUser[userId] = set;
        }

        set.Add(siteId);
    }

    public Task<ApplicationUser?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
        => Task.FromResult(_users.FirstOrDefault(u => u.Id == id && !u.IsDeleted));

    public Task<(IReadOnlyList<ApplicationUser> Items, int TotalCount)> ListAsync(
        string? search, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        IEnumerable<ApplicationUser> query = _users.Where(u => !u.IsDeleted);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(u =>
                u.FullName.Contains(term, StringComparison.OrdinalIgnoreCase)
                || (u.Email ?? string.Empty).Contains(term, StringComparison.OrdinalIgnoreCase));
        }

        var ordered = query.OrderBy(u => u.FullName).ToList();
        var items = ordered.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        return Task.FromResult(((IReadOnlyList<ApplicationUser>)items, ordered.Count));
    }

    public Task<IReadOnlyList<int>> GetSiteIdsAsync(string userId, CancellationToken cancellationToken = default)
        => Task.FromResult((IReadOnlyList<int>)(_sitesByUser.TryGetValue(userId, out var set)
            ? set.ToList()
            : []));

    public Task<IReadOnlyDictionary<string, string?>> GetRoleNamesAsync(
        IReadOnlyCollection<string> userIds, CancellationToken cancellationToken = default)
    {
        IReadOnlyDictionary<string, string?> result = userIds
            .Where(_roleByUser.ContainsKey)
            .ToDictionary(id => id, id => _roleByUser[id]);
        return Task.FromResult(result);
    }

    public Task<string?> GetRoleNameAsync(string userId, CancellationToken cancellationToken = default)
        => Task.FromResult(_roleByUser.GetValueOrDefault(userId));

    public Task<bool> EmailExistsAsync(string email, string? excludeId, CancellationToken cancellationToken = default)
        => Task.FromResult(_users.Any(u =>
            !u.IsDeleted
            && string.Equals(u.Email, email, StringComparison.OrdinalIgnoreCase)
            && (excludeId == null || u.Id != excludeId)));

    public Task<bool> RoleExistsAsync(string roleName, CancellationToken cancellationToken = default)
        => Task.FromResult(_roles.Contains(roleName));

    public Task<bool> SitesExistAsync(IReadOnlyCollection<int> siteIds, CancellationToken cancellationToken = default)
    {
        var distinct = siteIds.Distinct().ToList();
        return Task.FromResult(distinct.Count > 0 && distinct.All(_sites.ContainsKey));
    }

    public Task<bool> IsSiteLinkedAsync(string userId, int siteId, CancellationToken cancellationToken = default)
        => Task.FromResult(_sitesByUser.TryGetValue(userId, out var set) && set.Contains(siteId));

    public Task<(IReadOnlyList<Site> Items, int TotalCount)> ListLinkedSitesAsync(
        string userId, string? search, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var linked = _sitesByUser.TryGetValue(userId, out var set) ? set : [];
        IEnumerable<Site> query = linked.Where(_sites.ContainsKey).Select(id => _sites[id]);

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

    public Task<IReadOnlyList<Site>> GetAvailableSitesAsync(string userId, CancellationToken cancellationToken = default)
    {
        var linked = _sitesByUser.TryGetValue(userId, out var set) ? set : [];
        IReadOnlyList<Site> result = _sites.Values
            .Where(s => s.IsActive && !linked.Contains(s.Id))
            .OrderBy(s => s.Name)
            .ToList();
        return Task.FromResult(result);
    }

    public Task<IReadOnlyList<Site>> GetActiveLinkedSitesAsync(string userId, CancellationToken cancellationToken = default)
    {
        var linked = _sitesByUser.TryGetValue(userId, out var set) ? set : [];
        IReadOnlyList<Site> result = linked
            .Where(_sites.ContainsKey)
            .Select(id => _sites[id])
            .Where(s => s.IsActive)
            .OrderBy(s => s.Name)
            .ToList();
        return Task.FromResult(result);
    }

    public Task<bool> IsSiteAvailableForUserAsync(string userId, int siteId, CancellationToken cancellationToken = default)
    {
        var linked = _sitesByUser.TryGetValue(userId, out var set) && set.Contains(siteId);
        var available = !linked && _sites.TryGetValue(siteId, out var site) && site.IsActive;
        return Task.FromResult(available);
    }

    public Task AddSiteLinkAsync(string userId, int siteId, CancellationToken cancellationToken = default)
    {
        LinkSite(userId, siteId);
        return Task.CompletedTask;
    }

    public Task RemoveSiteLinkAsync(string userId, int siteId, CancellationToken cancellationToken = default)
    {
        if (_sitesByUser.TryGetValue(userId, out var set))
        {
            set.Remove(siteId);
        }

        return Task.CompletedTask;
    }

    public Task<ApplicationUser> AddAsync(
        ApplicationUser user, string password, string roleName, IReadOnlyCollection<int> siteIds,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(user.Id))
        {
            user.Id = Guid.NewGuid().ToString();
        }

        _users.Add(user);
        _roleByUser[user.Id] = roleName;
        _sitesByUser[user.Id] = siteIds.Distinct().ToHashSet();
        return Task.FromResult(user);
    }

    public Task UpdateAsync(
        ApplicationUser user, string roleName, CancellationToken cancellationToken = default)
    {
        // O perfil (inclui DefaultSiteId) já está na referência do usuário no store;
        // a edição não altera as plantas associadas (isso é do sub-CRUD).
        _roleByUser[user.Id] = roleName;
        return Task.CompletedTask;
    }

    // Simula a regeneração do stamp: o valor muda a cada chamada, como faz o UserManager.
    public Task RegenerateSecurityStampAsync(
        ApplicationUser user, CancellationToken cancellationToken = default)
    {
        user.SecurityStamp = Guid.NewGuid().ToString();
        SecurityStampRegenerations++;
        return Task.CompletedTask;
    }

    // Quantas vezes o stamp foi regenerado (os testes verificam que só a desativação regenera).
    public int SecurityStampRegenerations { get; private set; }

    public Task DeleteAsync(ApplicationUser user, CancellationToken cancellationToken = default)
    {
        user.IsDeleted = true;
        return Task.CompletedTask;
    }
}
