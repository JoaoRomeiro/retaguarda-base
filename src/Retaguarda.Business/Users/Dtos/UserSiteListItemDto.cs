namespace Retaguarda.Business.Users.Dtos;

// Linha da listagem de plantas associadas a um usuário (sub-CRUD de Plantas).
public sealed class UserSiteListItemDto
{
    public int SiteId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
}
