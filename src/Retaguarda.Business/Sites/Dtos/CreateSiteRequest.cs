namespace Retaguarda.Business.Sites.Dtos;

// Dados de entrada para criar um site.
public sealed class CreateSiteRequest
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string TimeZone { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}
