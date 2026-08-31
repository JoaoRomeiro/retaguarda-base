namespace Retaguarda.Api.Models.Authentication;

// Corpo do POST /api/auth/login. Campos anuláveis: validados explicitamente no controller
// (o filtro automático de ModelState está desligado para padronizar os erros com "code").
public sealed record LoginRequest(string? Email, string? Password);

// Corpo do POST /api/auth/select-site.
public sealed record SelectSiteRequest(string? PreAuthToken, int SiteId);

// Corpo do POST /api/auth/refresh.
public sealed record RefreshRequest(string? RefreshToken);

// Corpo do POST /api/auth/logout.
public sealed record LogoutRequest(string? RefreshToken);
