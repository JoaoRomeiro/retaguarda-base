using Retaguarda.Data.Identity;
using Retaguarda.Data.Repositories;

namespace Retaguarda.UnitTests.Roles;

// Repositório em memória para testar RoleService/validators sem RoleManager/EF nem lib de mock.
internal sealed class FakeRoleRepository : IRoleRepository
{
    private readonly List<ApplicationRole> _store = [];
    private readonly Dictionary<string, int> _usersByRoleId = [];

    public IReadOnlyList<ApplicationRole> Store => _store;

    // Insere um papel já existente (ex.: papel interno) com o NormalizedName coerente.
    public ApplicationRole Seed(ApplicationRole role)
    {
        role.NormalizedName = Normalize(role.Name);
        _store.Add(role);
        return role;
    }

    // Define quantos usuários estão vinculados a um papel (guarda de exclusão).
    public void SetUsersInRole(string roleId, int count) => _usersByRoleId[roleId] = count;

    public Task<ApplicationRole?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
        => Task.FromResult(_store.FirstOrDefault(r => r.Id == id && !r.IsDeleted));

    public Task<(IReadOnlyList<ApplicationRole> Items, int TotalCount)> ListAsync(
        string? search, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        IEnumerable<ApplicationRole> query = _store.Where(r => !r.IsDeleted);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(r =>
                (r.Name ?? string.Empty).Contains(term, StringComparison.OrdinalIgnoreCase)
                || (r.Description ?? string.Empty).Contains(term, StringComparison.OrdinalIgnoreCase));
        }

        var ordered = query.OrderBy(r => r.Name).ToList();
        var items = ordered.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        return Task.FromResult(((IReadOnlyList<ApplicationRole>)items, ordered.Count));
    }

    public Task<bool> NameExistsAsync(string name, string? excludeId, CancellationToken cancellationToken = default)
    {
        var normalized = Normalize(name);
        return Task.FromResult(_store.Any(r =>
            r.NormalizedName == normalized && !r.IsDeleted && (excludeId == null || r.Id != excludeId)));
    }

    public Task<int> CountUsersInRoleAsync(string roleId, CancellationToken cancellationToken = default)
        => Task.FromResult(_usersByRoleId.TryGetValue(roleId, out var count) ? count : 0);

    // Permissões por papel, espelhando identity."RoleClaims" filtrado por ClaimType = permission.
    private readonly Dictionary<string, List<string>> _permissionsByRoleId = [];

    public Task<IReadOnlyList<string>> GetPermissionsAsync(string roleId, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<string>>(
            _permissionsByRoleId.TryGetValue(roleId, out var permissions) ? [.. permissions] : []);

    public Task SetPermissionsAsync(
        ApplicationRole role, IReadOnlyCollection<string> permissions, CancellationToken cancellationToken = default)
    {
        _permissionsByRoleId[role.Id] = [.. permissions];
        return Task.CompletedTask;
    }

    // Define as permissões já existentes de um papel (arranjo do teste, não operação do cadastro).
    public void SeedPermissions(string roleId, params string[] permissions)
        => _permissionsByRoleId[roleId] = [.. permissions];

    public Task<ApplicationRole> AddAsync(ApplicationRole role, CancellationToken cancellationToken = default)
    {
        // O ctor do IdentityRole já gera o Id (GUID); aqui só normalizamos e guardamos.
        role.NormalizedName = Normalize(role.Name);
        _store.Add(role);
        return Task.FromResult(role);
    }

    public Task UpdateAsync(ApplicationRole role, CancellationToken cancellationToken = default)
    {
        // Mantém o NormalizedName coerente após eventual mudança de nome (como o RoleManager).
        role.NormalizedName = Normalize(role.Name);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(ApplicationRole role, CancellationToken cancellationToken = default)
    {
        role.IsDeleted = true;
        return Task.CompletedTask;
    }

    // Normalização equivalente à padrão do Identity (upper invariant).
    private static string? Normalize(string? value) => value?.ToUpperInvariant();
}
