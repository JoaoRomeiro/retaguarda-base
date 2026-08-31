using Retaguarda.Business.Sites.Dtos;
using Retaguarda.Shared.Models;

namespace Retaguarda.Web.Models.Sites;

// Modelo da tela de listagem: página de resultados + termo de busca corrente.
public sealed class SiteIndexViewModel
{
    public required PagedResult<SiteListItemDto> Sites { get; init; }
    public string? Search { get; init; }
}
