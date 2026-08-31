namespace Retaguarda.Business.Users.Dtos;

// Linha enxuta para a listagem (busca paginada).
public sealed class UserListItemDto
{
    public string Id { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? RoleName { get; set; }
    public string? DefaultSiteName { get; set; }
    public bool IsActive { get; set; }
}
