namespace Retaguarda.Shared.Contracts;

/// <summary>
/// Entidade com trilha de auditoria: quem/quando criou e atualizou.
/// Os campos são carimbados automaticamente no SaveChanges (§5.2, §6.2).
/// </summary>
public interface IAuditable
{
    DateTime CreatedAt { get; set; }
    string? CreatedById { get; set; }
    DateTime? UpdatedAt { get; set; }
    string? UpdatedById { get; set; }
}
