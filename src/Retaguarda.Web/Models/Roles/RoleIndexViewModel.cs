using Retaguarda.Business.Roles.Dtos;
using Retaguarda.Shared.Models;

namespace Retaguarda.Web.Models.Roles;

// Modelo da tela de listagem: página de resultados + termo de busca corrente.
public sealed class RoleIndexViewModel
{
    public required PagedResult<RoleListItemDto> Roles { get; init; }
    public string? Search { get; init; }
}
