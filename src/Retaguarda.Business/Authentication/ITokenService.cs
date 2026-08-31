namespace Retaguarda.Business.Authentication;

/// <summary>
/// Emissão e validação dos tokens JWT da API. Definido em Business; a
/// implementação concreta (JWT) vive na camada Api, que referencia o pacote JwtBearer.
/// </summary>
public interface ITokenService
{
    // Token de pré-autenticação: prova a identidade só para a etapa de seleção de planta.
    string CreatePreAuthToken(string userId);

    // Valida o token de pré-autenticação. Retorna o userId, ou null se inválido/expirado.
    string? ValidatePreAuthToken(string token);

    // Access token definitivo, já vinculado à planta ativa e aos papéis do usuário.
    AccessTokenResult CreateAccessToken(
        string userId, int siteId, string siteName, IReadOnlyCollection<string> roles);
}

// Access token emitido e o instante (UTC) em que expira.
public sealed record AccessTokenResult(string Token, DateTimeOffset ExpiresAt);
