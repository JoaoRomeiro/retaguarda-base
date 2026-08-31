using Retaguarda.Business.Users.Dtos;
using Retaguarda.Shared.Models;

namespace Retaguarda.Business.Users;

/// <summary>
/// Sub-CRUD das plantas associadas a um usuário (Read/Create/Delete). A planta padrão
/// não pode ser removida; a lista de associação é separada da edição do usuário.
/// </summary>
public interface IUserSiteService
{
    Task<PagedResult<UserSiteListItemDto>> ListAsync(
        string userId, string? search, int page, int pageSize, CancellationToken cancellationToken = default);

    // Plantas que ainda podem ser associadas (ativas e não vinculadas).
    Task<IReadOnlyList<AvailableSiteDto>> GetAvailableSitesAsync(
        string userId, CancellationToken cancellationToken = default);

    // Lança ValidationException se a planta for inválida/indisponível.
    Task AddAsync(AssociateSiteRequest request, CancellationToken cancellationToken = default);

    Task<SiteUnlinkResult> RemoveAsync(string userId, int siteId, CancellationToken cancellationToken = default);
}
