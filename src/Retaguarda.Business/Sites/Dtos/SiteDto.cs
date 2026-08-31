namespace Retaguarda.Business.Sites.Dtos;

// Representação completa do site para detalhe/edição.
public sealed class SiteDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string TimeZone { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}
