using Retaguarda.Data.Entities;
using Retaguarda.Data.Repositories;

namespace Retaguarda.UnitTests.Users;

// Repositório de refresh tokens em memória, para testar a revogação em massa disparada pela
// desativação do usuário sem EF nem mocks.
internal sealed class FakeRefreshTokenRepository : IRefreshTokenRepository
{
    private readonly List<RefreshToken> _tokens = [];

    public IReadOnlyList<RefreshToken> Tokens => _tokens;

    // Quantas vezes a revogação em massa foi chamada (mesmo sem tokens a revogar).
    public int RevokeAllCalls { get; private set; }

    // Semeia um token ativo de um usuário (cenário: sessão da Api aberta).
    public void SeedActive(string userId, string tokenHash, int siteId = 1) =>
        _tokens.Add(new RefreshToken
        {
            Id = _tokens.Count + 1,
            TokenHash = tokenHash,
            UserId = userId,
            SiteId = siteId,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(14),
        });

    public Task AddAsync(RefreshToken token, CancellationToken cancellationToken = default)
    {
        _tokens.Add(token);
        return Task.CompletedTask;
    }

    public Task<RefreshToken?> GetActiveByHashAsync(
        string tokenHash, CancellationToken cancellationToken = default)
        => Task.FromResult(_tokens.FirstOrDefault(t =>
            t.TokenHash == tokenHash && t.RevokedAt == null && t.ExpiresAt > DateTime.UtcNow));

    public Task RevokeAsync(RefreshToken token, CancellationToken cancellationToken = default)
    {
        token.RevokedAt = DateTime.UtcNow;
        return Task.CompletedTask;
    }

    public Task RotateAsync(
        RefreshToken oldToken, RefreshToken newToken, CancellationToken cancellationToken = default)
    {
        oldToken.RevokedAt = DateTime.UtcNow;
        _tokens.Add(newToken);
        return Task.CompletedTask;
    }

    public Task<int> RevokeAllForUserAsync(string userId, CancellationToken cancellationToken = default)
    {
        RevokeAllCalls++;

        var now = DateTime.UtcNow;
        var affected = _tokens
            .Where(t => t.UserId == userId && t.RevokedAt == null && t.ExpiresAt > now)
            .ToList();

        foreach (var token in affected)
        {
            token.RevokedAt = now;
        }

        return Task.FromResult(affected.Count);
    }
}
