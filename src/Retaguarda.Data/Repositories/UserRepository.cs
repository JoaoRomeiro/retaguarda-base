using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Retaguarda.Data.Identity;

namespace Retaguarda.Data.Repositories;

// Implementação EF Core + Identity. O global query filter (IsDeleted == false) e o
// interceptor de auditoria/soft delete valem para todas as operações. Create/Update do
// usuário e vínculo de role passam pelo UserManager; plantas e soft delete pelo DbContext.
public sealed class UserRepository : IUserRepository
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public UserRepository(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    public Task<ApplicationUser?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
        => _db.Users.Include(u => u.DefaultSite).FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

    public async Task<(IReadOnlyList<ApplicationUser> Items, int TotalCount)> ListAsync(
        string? search, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        IQueryable<ApplicationUser> query = _db.Users.AsNoTracking().Include(u => u.DefaultSite);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = $"%{search.Trim()}%";
            query = query.Where(u =>
                EF.Functions.Like(u.FullName, term) || EF.Functions.Like(u.Email!, term));
        }

        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(u => u.FullName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    public async Task<IReadOnlyList<int>> GetSiteIdsAsync(string userId, CancellationToken cancellationToken = default)
        => await _db.UserSites
            .Where(us => us.UserId == userId)
            .Select(us => us.SiteId)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyDictionary<string, string?>> GetRoleNamesAsync(
        IReadOnlyCollection<string> userIds, CancellationToken cancellationToken = default)
    {
        // Uma role por usuário: join UserRoles × Roles, sem N+1.
        var pairs = await (
            from ur in _db.UserRoles
            join r in _db.Roles on ur.RoleId equals r.Id
            where userIds.Contains(ur.UserId)
            select new { ur.UserId, r.Name }).ToListAsync(cancellationToken);

        return pairs
            .GroupBy(p => p.UserId)
            .ToDictionary(g => g.Key, g => (string?)g.First().Name);
    }

    public async Task<string?> GetRoleNameAsync(string userId, CancellationToken cancellationToken = default)
        => await (
            from ur in _db.UserRoles
            join r in _db.Roles on ur.RoleId equals r.Id
            where ur.UserId == userId
            select r.Name).FirstOrDefaultAsync(cancellationToken);

    public Task<bool> EmailExistsAsync(string email, string? excludeId, CancellationToken cancellationToken = default)
    {
        var normalized = _userManager.NormalizeEmail(email);
        return _db.Users.AnyAsync(
            u => u.NormalizedEmail == normalized && (excludeId == null || u.Id != excludeId),
            cancellationToken);
    }

    public Task<bool> RoleExistsAsync(string roleName, CancellationToken cancellationToken = default)
    {
        var normalized = roleName.ToUpperInvariant();  // normalização padrão do Identity
        return _db.Roles.AnyAsync(r => r.NormalizedName == normalized, cancellationToken);
    }

    public async Task<bool> SitesExistAsync(IReadOnlyCollection<int> siteIds, CancellationToken cancellationToken = default)
    {
        var distinct = siteIds.Distinct().ToList();
        if (distinct.Count == 0)
        {
            return false;
        }

        var found = await _db.Sites.CountAsync(s => distinct.Contains(s.Id), cancellationToken);
        return found == distinct.Count;
    }

    public Task<bool> IsSiteLinkedAsync(string userId, int siteId, CancellationToken cancellationToken = default)
        => _db.UserSites.AnyAsync(us => us.UserId == userId && us.SiteId == siteId, cancellationToken);

    public async Task<(IReadOnlyList<Entities.Site> Items, int TotalCount)> ListLinkedSitesAsync(
        string userId, string? search, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        // Plantas associadas (join UserSites × Sites; o filtro de soft delete de Sites já se aplica).
        IQueryable<Entities.Site> query =
            from us in _db.UserSites
            join s in _db.Sites on us.SiteId equals s.Id
            where us.UserId == userId
            select s;

        query = query.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
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

    public async Task<IReadOnlyList<Entities.Site>> GetAvailableSitesAsync(
        string userId, CancellationToken cancellationToken = default)
    {
        var linked = _db.UserSites.Where(us => us.UserId == userId).Select(us => us.SiteId);
        return await _db.Sites.AsNoTracking()
            .Where(s => s.IsActive && !linked.Contains(s.Id))
            .OrderBy(s => s.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Entities.Site>> GetActiveLinkedSitesAsync(
        string userId, CancellationToken cancellationToken = default)
    {
        IQueryable<Entities.Site> query =
            from us in _db.UserSites
            join s in _db.Sites on us.SiteId equals s.Id
            where us.UserId == userId && s.IsActive
            select s;

        return await query.AsNoTracking().OrderBy(s => s.Name).ToListAsync(cancellationToken);
    }

    public async Task<bool> IsSiteAvailableForUserAsync(
        string userId, int siteId, CancellationToken cancellationToken = default)
    {
        if (await IsSiteLinkedAsync(userId, siteId, cancellationToken))
        {
            return false;
        }

        return await _db.Sites.AnyAsync(s => s.Id == siteId && s.IsActive, cancellationToken);
    }

    public async Task AddSiteLinkAsync(string userId, int siteId, CancellationToken cancellationToken = default)
    {
        _db.UserSites.Add(new UserSite { UserId = userId, SiteId = siteId });
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveSiteLinkAsync(string userId, int siteId, CancellationToken cancellationToken = default)
    {
        var link = await _db.UserSites
            .FirstOrDefaultAsync(us => us.UserId == userId && us.SiteId == siteId, cancellationToken);
        if (link is not null)
        {
            _db.UserSites.Remove(link);  // o vínculo é uma tabela de associação (hard delete do link)
            await _db.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<ApplicationUser> AddAsync(
        ApplicationUser user, string password, string roleName, IReadOnlyCollection<int> siteIds,
        CancellationToken cancellationToken = default)
    {
        EnsureSucceeded(await _userManager.CreateAsync(user, password));
        EnsureSucceeded(await _userManager.AddToRoleAsync(user, roleName));

        foreach (var siteId in siteIds.Distinct())
        {
            _db.UserSites.Add(new UserSite { UserId = user.Id, SiteId = siteId });
        }

        await _db.SaveChangesAsync(cancellationToken);
        return user;
    }

    public async Task UpdateAsync(
        ApplicationUser user, string roleName, CancellationToken cancellationToken = default)
    {
        // Perfil (FullName, IsActive, DefaultSiteId etc.).
        EnsureSucceeded(await _userManager.UpdateAsync(user));

        // Role única: troca o vínculo se mudou. As plantas associadas não são alteradas aqui.
        var currentRoles = await _userManager.GetRolesAsync(user);
        if (currentRoles.Count != 1 || !currentRoles.Contains(roleName))
        {
            if (currentRoles.Count > 0)
            {
                EnsureSucceeded(await _userManager.RemoveFromRolesAsync(user, currentRoles));
            }

            EnsureSucceeded(await _userManager.AddToRoleAsync(user, roleName));
        }
    }

    public async Task RegenerateSecurityStampAsync(
        ApplicationUser user, CancellationToken cancellationToken = default)
        => EnsureSucceeded(await _userManager.UpdateSecurityStampAsync(user));

    public async Task DeleteAsync(ApplicationUser user, CancellationToken cancellationToken = default)
    {
        _db.Users.Remove(user);  // interceptor converte em soft delete (vira UPDATE)
        await _db.SaveChangesAsync(cancellationToken);
    }

    // Os validators já cobrem e-mail/role/sites/senha antes; uma falha aqui é excepcional.
    private static void EnsureSucceeded(IdentityResult result)
    {
        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(e => $"{e.Code}: {e.Description}"));
            throw new InvalidOperationException($"Identity user operation failed: {errors}");
        }
    }
}
