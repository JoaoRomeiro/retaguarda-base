using Retaguarda.Shared.Contracts;

namespace Retaguarda.Data.Interceptors;

/// <summary>
/// Lógica pura de carimbo de auditoria/soft delete — sem dependência de EF Core,
/// para ser testável isoladamente. Usada pelo AuditableEntityInterceptor.
/// </summary>
public static class AuditStamper
{
    // Inclusão: registra quem/quando criou.
    public static void StampCreated(IAuditable entity, string? userId, DateTime utcNow)
    {
        entity.CreatedAt = utcNow;
        entity.CreatedById = userId;
    }

    // Edição: registra quem/quando atualizou.
    public static void StampUpdated(IAuditable entity, string? userId, DateTime utcNow)
    {
        entity.UpdatedAt = utcNow;
        entity.UpdatedById = userId;
    }

    // Exclusão lógica: marca como excluído (não remove fisicamente).
    public static void StampDeleted(ISoftDeletable entity, string? userId, DateTime utcNow)
    {
        entity.IsDeleted = true;
        entity.DeletedAt = utcNow;
        entity.DeletedById = userId;
    }

    // Inclusão multi-site: carimba a planta ativa quando o registro ainda não tem site
    // definido (SiteId == 0). Não sobrescreve um site já informado explicitamente (ex.: seed).
    public static void StampSite(ISiteScoped entity, int? siteId)
    {
        if (entity.SiteId == 0 && siteId is int active)
        {
            entity.SiteId = active;
        }
    }
}
