namespace Retaguarda.Business.Roles.Dtos;

// Linha enxuta para a listagem (busca paginada).
public sealed class RoleListItemDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsSystem { get; set; }
}
