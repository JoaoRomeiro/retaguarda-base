namespace Retaguarda.Web.Models.Users;

// Modelo da confirmação de remoção de uma planta associada.
public sealed class UserSiteDeleteViewModel
{
    public required string UserId { get; init; }
    public required int SiteId { get; init; }
    public required string SiteName { get; init; }
    public required string SiteCode { get; init; }
}
