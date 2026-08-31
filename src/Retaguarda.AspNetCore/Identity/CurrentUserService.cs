using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Retaguarda.Shared;
using Retaguarda.Shared.Contracts;

namespace Retaguarda.AspNetCore.Identity;

/// <summary>
/// Lê o usuário e o site correntes a partir das claims do request (cookie/JWT).
/// Vive aqui (e não em Shared) porque depende de HttpContext (§4.2).
/// </summary>
public sealed class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
        => _httpContextAccessor = httpContextAccessor;

    public string? UserId =>
        _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);

    public int? SiteId
    {
        get
        {
            // Planta ativa gravada no cookie pela tela de seleção (roadmap 2.2.1); null até selecionar.
            var raw = _httpContextAccessor.HttpContext?.User.FindFirstValue(RetaguardaClaims.SiteId);
            return int.TryParse(raw, out var siteId) ? siteId : null;
        }
    }
}
