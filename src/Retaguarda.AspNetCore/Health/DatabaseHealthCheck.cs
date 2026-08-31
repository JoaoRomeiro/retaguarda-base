using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Retaguarda.Data.Identity;

namespace Retaguarda.AspNetCore.Health;

/// <summary>
/// Readiness check: confirma que o banco (Postgres) está acessível via <c>CanConnectAsync</c>.
/// Marcado com a tag "ready" e exposto em <c>/health/ready</c> — separado do <c>/health</c> (liveness),
/// que não depende do banco (um blip do DB não deve derrubar/reiniciar o processo). Baseline §9.
/// </summary>
public sealed class DatabaseHealthCheck : IHealthCheck
{
    // Tag usada para incluir este check só no endpoint de readiness.
    public const string ReadyTag = "ready";

    private readonly ApplicationDbContext _db;

    public DatabaseHealthCheck(ApplicationDbContext db) => _db = db;

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _db.Database.CanConnectAsync(cancellationToken)
                ? HealthCheckResult.Healthy()
                : HealthCheckResult.Unhealthy("Database unreachable");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Database check failed", ex);
        }
    }
}
