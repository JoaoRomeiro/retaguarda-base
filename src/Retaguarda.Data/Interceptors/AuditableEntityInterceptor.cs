using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Retaguarda.Shared.Contracts;

namespace Retaguarda.Data.Interceptors;

/// <summary>
/// Carimba auditoria (Created/Updated) e converte exclusão física em soft delete
/// (IsDeleted/DeletedAt/DeletedById) a cada SaveChanges. Depende do usuário corrente.
/// </summary>
public sealed class AuditableEntityInterceptor : SaveChangesInterceptor
{
    private readonly ICurrentUserService _currentUser;

    public AuditableEntityInterceptor(ICurrentUserService currentUser) => _currentUser = currentUser;

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData, InterceptionResult<int> result)
    {
        Apply(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        Apply(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void Apply(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        var userId = _currentUser.UserId;
        var utcNow = DateTime.UtcNow;  // datas sempre em UTC (§6.2/§12.6)

        foreach (var entry in context.ChangeTracker.Entries())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    // Auditoria e, para entidades multi-site, carimbo da planta ativa.
                    if (entry.Entity is IAuditable created)
                    {
                        AuditStamper.StampCreated(created, userId, utcNow);
                    }

                    if (entry.Entity is ISiteScoped scoped)
                    {
                        AuditStamper.StampSite(scoped, _currentUser.SiteId);
                    }

                    break;

                case EntityState.Modified when entry.Entity is IAuditable updated:
                    AuditStamper.StampUpdated(updated, userId, utcNow);
                    break;

                case EntityState.Deleted when entry.Entity is ISoftDeletable deletable:
                    AuditStamper.StampDeleted(deletable, userId, utcNow);
                    entry.State = EntityState.Modified;  // soft delete: vira UPDATE
                    break;

                default:
                    break;
            }
        }
    }
}
