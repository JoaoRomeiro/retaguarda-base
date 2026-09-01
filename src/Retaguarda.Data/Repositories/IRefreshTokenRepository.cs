using Retaguarda.Data.Entities;

namespace Retaguarda.Data.Repositories;

/// <summary>
/// Acesso a dados dos refresh tokens da API. Tipado na entidade, vive em Data
/// para que a implementação não dependa de Business (§4.2).
/// </summary>
public interface IRefreshTokenRepository
{
    // Persiste um novo refresh token (hash + vínculo usuário/planta + expiração).
    Task AddAsync(RefreshToken token, CancellationToken cancellationToken = default);

    // Busca um refresh token ativo (não revogado e não expirado) pelo hash. Null se não existir.
    Task<RefreshToken?> GetActiveByHashAsync(string tokenHash, CancellationToken cancellationToken = default);

    // Revoga um token (logout). Marca RevokedAt e persiste.
    Task RevokeAsync(RefreshToken token, CancellationToken cancellationToken = default);

    // Rotação (refresh): revoga o token antigo e persiste o novo na mesma transação.
    Task RotateAsync(RefreshToken oldToken, RefreshToken newToken, CancellationToken cancellationToken = default);

    // Revoga TODOS os tokens ativos de um usuário (desativação da conta). Devolve quantos foram
    // revogados. Idempotente: sem tokens ativos, não faz nada e devolve 0.
    Task<int> RevokeAllForUserAsync(string userId, CancellationToken cancellationToken = default);
}
