namespace Retaguarda.Business.Users.Dtos;

// Dados de entrada para associar uma planta a um usuário.
public sealed class AssociateSiteRequest
{
    public string UserId { get; set; } = string.Empty;
    public int SiteId { get; set; }
}
