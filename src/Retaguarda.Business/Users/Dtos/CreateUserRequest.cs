namespace Retaguarda.Business.Users.Dtos;

// Dados de entrada para criar um usuário. Senha definida pelo admin; 1 role; a planta
// escolhida vira a planta padrão e a primeira associação. Plantas extras: sub-CRUD de Plantas.
public sealed class CreateUserRequest
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string RoleName { get; set; } = string.Empty;
    public int DefaultSiteId { get; set; }
    public bool IsActive { get; set; } = true;
}
