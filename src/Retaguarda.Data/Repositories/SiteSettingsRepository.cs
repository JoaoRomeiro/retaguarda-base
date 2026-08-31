using Microsoft.EntityFrameworkCore;
using Retaguarda.Data.Identity;
using Retaguarda.Shared.Contracts;

namespace Retaguarda.Data.Repositories;

// Lê as configurações da planta ativa. Sites não tem filtro por planta (é a raiz do multi-site),
// por isso o SiteId vem do usuário corrente e entra explicitamente na consulta.
public sealed class SiteSettingsRepository : ISiteSettingsRepository
{
    private readonly ApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public SiteSettingsRepository(ApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<SiteSettings> GetAsync(CancellationToken cancellationToken = default)
    {
        if (_currentUser.SiteId is not int siteId)
        {
            return SiteSettings.Defaults;
        }

        return await _db.Sites
            .AsNoTracking()
            .Where(s => s.Id == siteId)
            .Select(s => new SiteSettings(s.TimeZone))
            .FirstOrDefaultAsync(cancellationToken)
            ?? SiteSettings.Defaults;
    }
}
