namespace Retaguarda.Business.Users.Dtos;

// Representação completa do usuário para detalhe/edição.
public sealed class UserDto
{
    public string Id { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? RoleName { get; set; }
    public int DefaultSiteId { get; set; }
    public List<int> SiteIds { get; set; } = [];
    public bool IsActive { get; set; }
}
