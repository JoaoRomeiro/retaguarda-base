namespace Retaguarda.Business.Authentication.Dtos;

/// <summary>
/// Par de tokens emitido após a seleção da planta, com o contexto do usuário autenticado.
/// </summary>
public sealed record AuthTokens(
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAt,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAt,
    string UserId,
    int SiteId,
    string SiteName,
    IReadOnlyList<string> Roles);
