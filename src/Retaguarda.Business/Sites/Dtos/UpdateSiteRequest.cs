namespace Retaguarda.Business.Sites.Dtos;

// Dados de entrada para editar um site existente.
public sealed class UpdateSiteRequest
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string TimeZone { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}
