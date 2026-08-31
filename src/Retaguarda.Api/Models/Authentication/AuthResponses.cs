namespace Retaguarda.Api.Models.Authentication;

// Resposta do login (etapa 1): token de pré-autenticação + plantas selecionáveis.
public sealed class LoginResponse
{
    public required string PreAuthToken { get; init; }

    // Planta padrão do usuário, quando estiver entre as selecionáveis (sugestão de pré-seleção).
    public int? DefaultSiteId { get; init; }

    public required IReadOnlyList<SelectableSiteResponse> Sites { get; init; }
}

public sealed record SelectableSiteResponse(int Id, string Code, string Name);

// Resposta da seleção de planta (etapa 2): par de tokens + contexto do usuário.
public sealed class TokenResponse
{
    public required string AccessToken { get; init; }
    public required DateTimeOffset AccessTokenExpiresAt { get; init; }
    public required string RefreshToken { get; init; }
    public required DateTimeOffset RefreshTokenExpiresAt { get; init; }
    public string TokenType { get; init; } = "Bearer";
    public required UserContextResponse User { get; init; }
}

public sealed record UserContextResponse(string UserId, int SiteId, string SiteName, IReadOnlyList<string> Roles);

// Resposta do GET /api/auth/me: eco do contexto lido das claims do access token.
public sealed record MeResponse(string? UserId, int? SiteId, string? SiteName, IReadOnlyList<string> Roles);
