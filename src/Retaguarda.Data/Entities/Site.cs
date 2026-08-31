namespace Retaguarda.Data.Entities;

/// <summary>
/// Planta (unidade operacional). Raiz do isolamento multi-site: NÃO possui SiteId, pois não se
/// auto-referencia. Toda entidade do domínio referencia uma planta e é filtrada por ela.
/// </summary>
public sealed class Site : AuditableEntity
{
    public int Id { get; set; }

    // Nome da planta.
    public string Name { get; set; } = string.Empty;

    // Código curto e único (ex.: "SP01").
    public string Code { get; set; } = string.Empty;

    // Fuso IANA para apresentação de datas (ex.: "America/Sao_Paulo").
    public string TimeZone { get; set; } = string.Empty;

    // Site ativo? Inativos não recebem novas operações.
    public bool IsActive { get; set; } = true;
}
