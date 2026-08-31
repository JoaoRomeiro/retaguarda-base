namespace Retaguarda.Shared.Contracts;

/// <summary>
/// Marca entidades pertencentes a um site (isolamento multi-site, §4.4).
/// O `Site` NÃO implementa esta interface — é a raiz do isolamento (§6.3.1).
/// O Global Query Filter por SiteId será aplicado às entidades que a implementam.
/// </summary>
public interface ISiteScoped
{
    int SiteId { get; set; }
}
