using Microsoft.EntityFrameworkCore;
using Retaguarda.Data.Entities;
using Retaguarda.Data.Identity;

namespace Retaguarda.Data.Repositories;

// Implementação EF Core. RefreshTokens não passam pelo interceptor de auditoria/soft delete
// (não implementam IAuditable/ISoftDeletable) — CreatedAt é definido pelo serviço.
public sealed class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly ApplicationDbContext _db;

    public RefreshTokenRepository(ApplicationDbContext db) => _db = db;

    public async Task AddAsync(RefreshToken token, CancellationToken cancellationToken = default)
    {
        _db.RefreshTokens.Add(token);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<RefreshToken?> GetActiveByHashAsync(
        string tokenHash, CancellationToken cancellationToken = default)
    {
        // Só considera tokens utilizáveis: não revogados e ainda dentro da validade.
        var now = DateTime.UtcNow;
        return await _db.RefreshTokens.FirstOrDefaultAsync(
            t => t.TokenHash == tokenHash && t.RevokedAt == null && t.ExpiresAt > now,
            cancellationToken);
    }

    public async Task RevokeAsync(RefreshToken token, CancellationToken cancellationToken = default)
    {
        token.RevokedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task RotateAsync(
        RefreshToken oldToken, RefreshToken newToken, CancellationToken cancellationToken = default)
    {
        // Revoga o antigo e grava o novo numa única transação (SaveChanges atômico).
        oldToken.RevokedAt = DateTime.UtcNow;
        _db.RefreshTokens.Add(newToken);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
