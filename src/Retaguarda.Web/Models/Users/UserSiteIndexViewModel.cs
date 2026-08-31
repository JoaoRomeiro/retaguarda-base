using Retaguarda.Business.Users.Dtos;
using Retaguarda.Shared.Models;

namespace Retaguarda.Web.Models.Users;

// Modelo da listagem de plantas associadas a um usuário (sub-CRUD).
public sealed class UserSiteIndexViewModel
{
    public required string UserId { get; init; }
    public required string UserName { get; init; }
    public required PagedResult<UserSiteListItemDto> Sites { get; init; }
    public string? Search { get; init; }
}
