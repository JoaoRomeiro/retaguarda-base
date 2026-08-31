namespace Retaguarda.Shared.Contracts;

/// <summary>
/// Entidade com exclusão lógica (soft delete): nunca é removida fisicamente;
/// o SaveChanges converte a exclusão em marcação (§5.2, §6.2).
/// </summary>
public interface ISoftDeletable
{
    bool IsDeleted { get; set; }
    DateTime? DeletedAt { get; set; }
    string? DeletedById { get; set; }
}
