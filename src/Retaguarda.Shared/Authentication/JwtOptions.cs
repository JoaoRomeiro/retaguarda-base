namespace Retaguarda.Shared.Authentication;

/// <summary>
/// Configuração dos tokens JWT da API. Vinculada à seção "Jwt" da configuração; a chave de
/// assinatura vem de variável de ambiente em produção (JWT__SigningKey) e nunca é commitada.
/// </summary>
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    // Chave simétrica de assinatura (HMAC-SHA256). Mínimo de 32 bytes.
    public string SigningKey { get; set; } = string.Empty;

    // Emissor e público (audience) do access token.
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;

    // Tempo de vida do access token (curto).
    public int AccessTokenMinutes { get; set; } = 30;

    // Tempo de vida do token de pré-autenticação (só vale para selecionar a planta).
    public int PreAuthTokenMinutes { get; set; } = 5;

    // Tempo de vida do refresh token (longo).
    public int RefreshTokenDays { get; set; } = 14;

    // Audience exclusiva do token de pré-autenticação: garante que ele NÃO seja aceito
    // pelo middleware JWT que protege os endpoints de negócio (audience diferente).
    public string PreAuthAudience => $"{Audience}/site-selection";
}
