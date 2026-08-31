using Retaguarda.Business.Users.Dtos;
using Retaguarda.Shared.Models;

namespace Retaguarda.Web.Models.Users;

// Modelo da tela de listagem: página de resultados + termo de busca corrente.
public sealed class UserIndexViewModel
{
    public required PagedResult<UserListItemDto> Users { get; init; }
    public string? Search { get; init; }
}
