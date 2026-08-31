namespace Retaguarda.Business.Roles.Dtos;

// Dados de entrada para criar um papel.
public sealed class CreateRoleRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}
