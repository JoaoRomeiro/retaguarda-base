using Microsoft.EntityFrameworkCore;
using Retaguarda.Data.Entities;
using Retaguarda.Data.Identity;

namespace Retaguarda.Data.Repositories;

// Implementação EF Core. O global query filter (IsDeleted == false) e o interceptor
// de auditoria/soft delete do ApplicationDbContext valem para todas as operações.
public sealed class SiteRepository : ISiteRepository
{
    private readonly ApplicationDbContext _db;

    public SiteRepository(ApplicationDbContext db) => _db = db;

    public Task<Site?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        => _db.Sites.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

    public async Task<(IReadOnlyList<Site> Items, int TotalCount)> ListAsync(
        string? search, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = _db.Sites.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            // LIKE explícito (EF.Functions) evita o analyzer de StringComparison e
            // mantém a busca traduzível para SQL; collation CI_AI já é case-insensitive.
            var term = $"%{search.Trim()}%";
            query = query.Where(s => EF.Functions.Like(s.Name, term) || EF.Functions.Like(s.Code, term));
        }

        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(s => s.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    public Task<bool> CodeExistsAsync(string code, int? excludeId, CancellationToken cancellationToken = default)
        // O índice único é filtrado por IsDeleted = 0; a checagem olha só os ativos (o global
        // query filter já exclui os excluídos), permitindo reaproveitar códigos de plantas excluídas.
        => _db.Sites
            .AnyAsync(s => s.Code == code && (excludeId == null || s.Id != excludeId), cancellationToken);

    public Task<bool> NameExistsAsync(string name, int? excludeId, CancellationToken cancellationToken = default)
        // Mesma estratégia do Code: índice único filtrado por IsDeleted = 0; o global query filter
        // já remove os excluídos, permitindo reaproveitar o nome de uma planta excluída logicamente.
        => _db.Sites
            .AnyAsync(s => s.Name == name && (excludeId == null || s.Id != excludeId), cancellationToken);

    public async Task<Site> AddAsync(Site site, CancellationToken cancellationToken = default)
    {
        _db.Sites.Add(site);
        await _db.SaveChangesAsync(cancellationToken);
        return site;
    }

    public async Task UpdateAsync(Site site, CancellationToken cancellationToken = default)
    {
        _db.Sites.Update(site);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Site site, CancellationToken cancellationToken = default)
    {
        _db.Sites.Remove(site);  // interceptor converte em soft delete (vira UPDATE)
        await _db.SaveChangesAsync(cancellationToken);
    }
}
