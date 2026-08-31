namespace Retaguarda.Business.Roles.Dtos;

// Dados de entrada para editar um papel existente. Id é string (PK do Identity).
public sealed class UpdateRoleRequest
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}
