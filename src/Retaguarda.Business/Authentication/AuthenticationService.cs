using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Retaguarda.Business.Authentication.Dtos;
using Retaguarda.Business.Users;
using Retaguarda.Data.Entities;
using Retaguarda.Data.Identity;
using Retaguarda.Data.Repositories;
using Retaguarda.Shared.Authentication;

namespace Retaguarda.Business.Authentication;

// Orquestra o login em duas etapas. Validação de credenciais (com lockout e bloqueio de
// inativos) via UserManager; emissão de tokens via ITokenService; persistência do refresh
// token (como hash) via IRefreshTokenRepository.
public sealed class AuthenticationService : IAuthenticationService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ISiteSelectionService _siteSelection;
    private readonly ITokenService _tokenService;
    private readonly IRefreshTokenRepository _refreshTokens;
    private readonly JwtOptions _jwtOptions;
    private readonly ILogger<AuthenticationService> _logger;

    public AuthenticationService(
        UserManager<ApplicationUser> userManager,
        ISiteSelectionService siteSelection,
        ITokenService tokenService,
        IRefreshTokenRepository refreshTokens,
        IOptions<JwtOptions> jwtOptions,
        ILogger<AuthenticationService> logger)
    {
        _userManager = userManager;
        _siteSelection = siteSelection;
        _tokenService = tokenService;
        _refreshTokens = refreshTokens;
        _jwtOptions = jwtOptions.Value;
        _logger = logger;
    }

    public async Task<LoginOutcome> LoginAsync(
        string email, string password, CancellationToken cancellationToken = default)
    {
        // Usuário inexistente ou inativo: mensagem neutra (anti-enumeração, como no Web).
        var user = await _userManager.FindByEmailAsync(email);
        if (user is null || !user.IsActive)
        {
            return LoginOutcome.InvalidCredentials();
        }

        if (await _userManager.IsLockedOutAsync(user))
        {
            return LoginOutcome.LockedOut();
        }

        if (!await _userManager.CheckPasswordAsync(user, password))
        {
            // Conta tentativas e bloqueia ao atingir o limite (lockoutOnFailure, como no Web).
            await _userManager.AccessFailedAsync(user);
            return await _userManager.IsLockedOutAsync(user)
                ? LoginOutcome.LockedOut()
                : LoginOutcome.InvalidCredentials();
        }

        await _userManager.ResetAccessFailedCountAsync(user);

        var sites = await _siteSelection.GetSelectableSitesAsync(user.Id, cancellationToken);
        var preAuthToken = _tokenService.CreatePreAuthToken(user.Id);
        return LoginOutcome.Success(preAuthToken, sites, user.DefaultSiteId);
    }

    public async Task<SelectSiteOutcome> SelectSiteAsync(
        string preAuthToken, int siteId, CancellationToken cancellationToken = default)
    {
        var userId = _tokenService.ValidatePreAuthToken(preAuthToken);
        if (userId is null)
        {
            return SelectSiteOutcome.InvalidToken();
        }

        // A planta escolhida precisa estar entre as ativas e associadas ao usuário (§5.6).
        var sites = await _siteSelection.GetSelectableSitesAsync(userId, cancellationToken);
        var site = sites.FirstOrDefault(s => s.Id == siteId);
        if (site is null)
        {
            return SelectSiteOutcome.SiteNotAllowed();
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return SelectSiteOutcome.InvalidToken();
        }

        IReadOnlyList<string> roles = [.. await _userManager.GetRolesAsync(user)];
        var (tokens, entity) = IssueTokens(userId, siteId, site.Name, roles);
        await _refreshTokens.AddAsync(entity, cancellationToken);
        return SelectSiteOutcome.Success(tokens);
    }

    public async Task<RefreshOutcome> RefreshAsync(
        string refreshToken, CancellationToken cancellationToken = default)
    {
        // Localiza o token ativo pelo hash (o valor bruto nunca é persistido).
        var hash = HashRefreshToken(refreshToken);
        var existing = await _refreshTokens.GetActiveByHashAsync(hash, cancellationToken);
        if (existing is null)
        {
            await DetectReuseAsync(hash, cancellationToken);
            return RefreshOutcome.Invalid();
        }

        var user = await _userManager.FindByIdAsync(existing.UserId);
        if (user is null || !user.IsActive)
        {
            return RefreshOutcome.Invalid();
        }

        // Revalida que o usuário ainda tem acesso à planta fixada na sessão: se o vínculo foi
        // removido (ou a planta desativada), a renovação falha e exige novo login.
        var sites = await _siteSelection.GetSelectableSitesAsync(existing.UserId, cancellationToken);
        var site = sites.FirstOrDefault(s => s.Id == existing.SiteId);
        if (site is null)
        {
            return RefreshOutcome.Invalid();
        }

        IReadOnlyList<string> roles = [.. await _userManager.GetRolesAsync(user)];
        var (tokens, newEntity) = IssueTokens(existing.UserId, existing.SiteId, site.Name, roles);

        // Rotação (Opção A): revoga o token usado e emite um novo par na mesma transação.
        await _refreshTokens.RotateAsync(existing, newEntity, cancellationToken);
        return RefreshOutcome.Success(tokens);
    }

    public async Task<LogoutOutcome> LogoutAsync(
        string refreshToken, string userId, CancellationToken cancellationToken = default)
    {
        var hash = HashRefreshToken(refreshToken);
        var existing = await _refreshTokens.GetActiveByHashAsync(hash, cancellationToken);

        // Só revoga se o token existir, estiver ativo e pertencer a quem está autenticado.
        if (existing is null || !string.Equals(existing.UserId, userId, StringComparison.Ordinal))
        {
            return LogoutOutcome.NotFound();
        }

        await _refreshTokens.RevokeAsync(existing, cancellationToken);
        return LogoutOutcome.Success();
    }

    // Um hash que existe mas NÃO está ativo é sinal de token vazado: o legítimo já rotacionou
    // (revogando este) e alguém está tentando usar a cópia antiga — ou o contrário, e quem
    // rotacionou foi o atacante. Não dá para saber qual dos dois é o dono, então derruba a
    // sessão inteira e força login novo. Só REVOGADO conta: token apenas expirado é validade
    // vencida, não reuso.
    private async Task DetectReuseAsync(string hash, CancellationToken cancellationToken)
    {
        var known = await _refreshTokens.GetByHashAsync(hash, cancellationToken);
        if (known?.RevokedAt is null)
        {
            return;
        }

        var revoked = await _refreshTokens.RevokeAllForUserAsync(known.UserId, cancellationToken);

        // Nunca logar o token nem o hash: o hash identifica a sessão e é comparável.
        _logger.LogWarning(
            "Refresh token reuse detected for user {UserId}; revoked {RevokedCount} active tokens",
            known.UserId,
            revoked);
    }

    // Monta o access token e a entidade do novo refresh token (hash + vínculo usuário/planta +
    // expiração). A persistência fica a cargo do chamador (AddAsync no select-site, RotateAsync
    // no refresh), que decide a transação adequada.
    private (AuthTokens Tokens, RefreshToken Entity) IssueTokens(
        string userId, int siteId, string siteName, IReadOnlyList<string> roles)
    {
        var access = _tokenService.CreateAccessToken(userId, siteId, siteName, roles);
        var (rawRefresh, refreshHash) = GenerateRefreshToken();
        var refreshExpiresAt = DateTimeOffset.UtcNow.AddDays(_jwtOptions.RefreshTokenDays);

        var entity = new RefreshToken
        {
            TokenHash = refreshHash,
            UserId = userId,
            SiteId = siteId,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = refreshExpiresAt.UtcDateTime,
        };

        var tokens = new AuthTokens(
            access.Token, access.ExpiresAt,
            rawRefresh, refreshExpiresAt,
            userId, siteId, siteName, roles);
        return (tokens, entity);
    }

    // Gera o refresh token: valor bruto aleatório (entregue ao cliente) e seu hash SHA-256
    // (único valor persistido). Comparações futuras refazem o hash do valor recebido.
    private static (string Raw, string Hash) GenerateRefreshToken()
    {
        var raw = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        return (raw, HashRefreshToken(raw));
    }

    // Hash determinístico (SHA-256, Base64) do valor bruto — usado tanto na emissão quanto na
    // busca por um token recebido.
    private static string HashRefreshToken(string raw)
        => Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(raw)));
}
