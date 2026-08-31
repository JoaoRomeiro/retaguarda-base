namespace Retaguarda.Business.Sites.Dtos;

// Linha enxuta para a listagem (busca paginada).
public sealed class SiteListItemDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}
