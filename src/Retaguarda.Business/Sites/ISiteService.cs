using Retaguarda.Business.Sites.Dtos;
using Retaguarda.Shared.Models;

namespace Retaguarda.Business.Sites;

/// <summary>
/// Casos de uso de <see cref="Dtos.SiteDto"/> (CRUD do cadastro de plantas).
/// Definido em Business e consumido por Web/Api (§4.2).
/// </summary>
public interface ISiteService
{
    Task<PagedResult<SiteListItemDto>> ListAsync(
        string? search, int page, int pageSize, CancellationToken cancellationToken = default);

    Task<SiteDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    // Lança FluentValidation.ValidationException se a entrada for inválida.
    Task<SiteDto> CreateAsync(CreateSiteRequest request, CancellationToken cancellationToken = default);

    // False se o site não existe; lança ValidationException se a entrada for inválida.
    Task<bool> UpdateAsync(UpdateSiteRequest request, CancellationToken cancellationToken = default);

    // False se o site não existe; exclusão é lógica (soft delete).
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
