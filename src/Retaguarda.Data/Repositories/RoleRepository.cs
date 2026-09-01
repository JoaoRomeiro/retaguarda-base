using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Retaguarda.Data.Identity;

namespace Retaguarda.Data.Repositories;

// Implementação EF Core + Identity. O global query filter (IsDeleted == false) e o
// interceptor de auditoria/soft delete do ApplicationDbContext valem para todas as
// operações. Create/Update passam pelo RoleManager (normaliza nome, gera stamp);
// listagem/consulta/exclusão lógica vão direto pelo DbContext.
public sealed class RoleRepository : IRoleRepository
{
    private readonly ApplicationDbContext _db;
    private readonly RoleManager<ApplicationRole> _roleManager;

    public RoleRepository(ApplicationDbContext db, RoleManager<ApplicationRole> roleManager)
    {
        _db = db;
        _roleManager = roleManager;
    }

    public Task<ApplicationRole?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
        => _db.Roles.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

    public async Task<(IReadOnlyList<ApplicationRole> Items, int TotalCount)> ListAsync(
        string? search, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = _db.Roles.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            // ILIKE (operador do PostgreSQL, exposto pelo Npgsql): busca traduzível para SQL e
            // insensível a maiúsculas. NÃO trocar por Like: a collation padrão do Postgres é
            // case-sensitive, e "matriz" deixaria de encontrar "Matriz". O termo é escapado
            // (SearchPattern) para que % e _ digitados valham como texto, não como curinga.
            var term = SearchPattern.Contains(search);
            query = query.Where(r =>
                EF.Functions.ILike(r.Name!, term, SearchPattern.EscapeCharacter)
                || EF.Functions.ILike(r.Description!, term, SearchPattern.EscapeCharacter));
        }

        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(r => r.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    public Task<bool> NameExistsAsync(string name, string? excludeId, CancellationToken cancellationToken = default)
    {
        // Compara pelo NormalizedName (mesma normalização do Identity). O índice único é
        // filtrado por IsDeleted = 0 e o global query filter exclui os removidos, permitindo
        // reaproveitar o nome de um papel excluído logicamente.
        var normalized = _roleManager.NormalizeKey(name);
        return _db.Roles.AnyAsync(
            r => r.NormalizedName == normalized && (excludeId == null || r.Id != excludeId),
            cancellationToken);
    }

    public Task<int> CountUsersInRoleAsync(string roleId, CancellationToken cancellationToken = default)
        => _db.UserRoles.CountAsync(ur => ur.RoleId == roleId, cancellationToken);

    public async Task<ApplicationRole> AddAsync(ApplicationRole role, CancellationToken cancellationToken = default)
    {
        var result = await _roleManager.CreateAsync(role);
        EnsureSucceeded(result);
        return role;
    }

    public async Task UpdateAsync(ApplicationRole role, CancellationToken cancellationToken = default)
    {
        var result = await _roleManager.UpdateAsync(role);
        EnsureSucceeded(result);
    }

    public async Task DeleteAsync(ApplicationRole role, CancellationToken cancellationToken = default)
    {
        _db.Roles.Remove(role);  // interceptor converte em soft delete (vira UPDATE)
        await _db.SaveChangesAsync(cancellationToken);
    }

    // O serviço já valida nome/unicidade antes; uma falha aqui é excepcional.
    private static void EnsureSucceeded(IdentityResult result)
    {
        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(e => $"{e.Code}: {e.Description}"));
            throw new InvalidOperationException($"Identity role operation failed: {errors}");
        }
    }
}
