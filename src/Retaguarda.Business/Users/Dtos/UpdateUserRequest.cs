namespace Retaguarda.Business.Users.Dtos;

// Dados de entrada para editar um usuário. E-mail (login) e senha não mudam aqui.
// A planta padrão deve estar entre as plantas associadas (geridas no sub-CRUD de Plantas);
// a edição não altera a lista de associações.
public sealed class UpdateUserRequest
{
    public string Id { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string RoleName { get; set; } = string.Empty;
    public int DefaultSiteId { get; set; }
    public bool IsActive { get; set; }
}
