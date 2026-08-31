using Retaguarda.Shared.Contracts;

namespace Retaguarda.Data.Entities;

/// <summary>
/// Base das entidades de negócio: auditoria + soft delete. Os campos são
/// carimbados automaticamente pelo AuditableEntityInterceptor a cada SaveChanges.
/// A PK fica em cada entidade concreta, pois o tipo varia (int/bigint).
/// </summary>
public abstract class AuditableEntity : IAuditable, ISoftDeletable
{
    // Auditoria (§6.2). *ById referenciam AspNetUsers.Id (string, nvarchar(450)).
    public DateTime CreatedAt { get; set; }
    public string? CreatedById { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedById { get; set; }

    // Soft delete (§6.2).
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string? DeletedById { get; set; }
}
