using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Localization;
using Retaguarda.Api.Infrastructure;
using Retaguarda.Api.Models.Authentication;
using Retaguarda.AspNetCore.Security;
using Retaguarda.Business.Authentication;
using Retaguarda.Business.Authentication.Dtos;
using Retaguarda.Shared;

namespace Retaguarda.Api.Controllers;

// Autenticação em duas etapas. login e select-site são anônimos; me exige
// um access token válido. Mensagens vêm de Resources/Controllers/AuthController.<culture>.resx,
// cujas chaves são exatamente os "code" devolvidos no ProblemDetails de erro.
[Route("api/auth")]
public sealed class AuthController : ApiControllerBase
{
    private readonly IAuthenticationService _authentication;
    private readonly IStringLocalizer<AuthController> _localizer;

    public AuthController(
        IAuthenticationService authentication, IStringLocalizer<AuthController> localizer)
    {
        _authentication = authentication;
        _localizer = localizer;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting(AuthRateLimiting.CredentialsPolicy)]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        if (request is null
            || string.IsNullOrWhiteSpace(request.Email)
            || string.IsNullOrWhiteSpace(request.Password))
        {
            return ValidationError();
        }

        var outcome = await _authentication.LoginAsync(request.Email, request.Password, cancellationToken);
        return outcome.Status switch
        {
            LoginStatus.Success => OkData(MapLogin(outcome)),
            LoginStatus.LockedOut => ProblemWithCode(
                StatusCodes.Status429TooManyRequests, "user_locked_out", _localizer["user_locked_out"]),
            _ => ProblemWithCode(
                StatusCodes.Status401Unauthorized, "invalid_credentials", _localizer["invalid_credentials"]),
        };
    }

    [HttpPost("select-site")]
    [AllowAnonymous]
    [EnableRateLimiting(AuthRateLimiting.CredentialsPolicy)]
    public async Task<IActionResult> SelectSite(
        [FromBody] SelectSiteRequest request, CancellationToken cancellationToken)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.PreAuthToken) || request.SiteId <= 0)
        {
            return ValidationError();
        }

        var outcome = await _authentication.SelectSiteAsync(
            request.PreAuthToken, request.SiteId, cancellationToken);
        return outcome.Status switch
        {
            SelectSiteStatus.Success => OkData(MapTokens(outcome.Tokens!)),
            SelectSiteStatus.SiteNotAllowed => ProblemWithCode(
                StatusCodes.Status403Forbidden, "site_not_allowed", _localizer["site_not_allowed"]),
            _ => ProblemWithCode(
                StatusCodes.Status401Unauthorized, "invalid_pre_auth_token", _localizer["invalid_pre_auth_token"]),
        };
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    [EnableRateLimiting(AuthRateLimiting.RefreshPolicy)]
    public async Task<IActionResult> Refresh(
        [FromBody] RefreshRequest request, CancellationToken cancellationToken)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            return ValidationError();
        }

        var outcome = await _authentication.RefreshAsync(request.RefreshToken, cancellationToken);
        return outcome.Status switch
        {
            RefreshStatus.Success => OkData(MapTokens(outcome.Tokens!)),
            _ => ProblemWithCode(
                StatusCodes.Status401Unauthorized, "invalid_refresh_token", _localizer["invalid_refresh_token"]),
        };
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout(
        [FromBody] LogoutRequest request, CancellationToken cancellationToken)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            return ValidationError();
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return ValidationError();
        }

        var outcome = await _authentication.LogoutAsync(request.RefreshToken, userId, cancellationToken);
        return outcome.Status switch
        {
            LogoutStatus.Success => NoContent(),
            _ => ProblemWithCode(
                StatusCodes.Status404NotFound, "refresh_token_not_found", _localizer["refresh_token_not_found"]),
        };
    }

    [HttpGet("me")]
    [Authorize]
    public IActionResult Me()
    {
        var roles = User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
        var siteId = int.TryParse(User.FindFirstValue(RetaguardaClaims.SiteId), out var parsed)
            ? parsed
            : (int?)null;

        return OkData(new MeResponse(
            User.FindFirstValue(ClaimTypes.NameIdentifier),
            siteId,
            User.FindFirstValue(RetaguardaClaims.SiteName),
            roles));
    }

    private IActionResult ValidationError()
        => ProblemWithCode(StatusCodes.Status400BadRequest, "validation_error", _localizer["validation_error"]);

    private static LoginResponse MapLogin(LoginOutcome outcome)
    {
        var sites = outcome.Sites
            .Select(s => new SelectableSiteResponse(s.Id, s.Code, s.Name))
            .ToList();

        // Só sugere a planta padrão se ela estiver entre as selecionáveis.
        int? defaultSiteId = outcome.Sites.Any(s => s.Id == outcome.DefaultSiteId)
            ? outcome.DefaultSiteId
            : null;

        return new LoginResponse
        {
            PreAuthToken = outcome.PreAuthToken!,
            DefaultSiteId = defaultSiteId,
            Sites = sites,
        };
    }

    private static TokenResponse MapTokens(AuthTokens tokens) => new()
    {
        AccessToken = tokens.AccessToken,
        AccessTokenExpiresAt = tokens.AccessTokenExpiresAt,
        RefreshToken = tokens.RefreshToken,
        RefreshTokenExpiresAt = tokens.RefreshTokenExpiresAt,
        User = new UserContextResponse(tokens.UserId, tokens.SiteId, tokens.SiteName, tokens.Roles),
    };
}
