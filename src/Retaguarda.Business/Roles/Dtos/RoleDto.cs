namespace Retaguarda.Business.Roles.Dtos;

// Representação completa do papel para detalhe/edição. IsSystem é somente-leitura
// (papéis internos têm o nome bloqueado e não podem ser excluídos).
public sealed class RoleDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsSystem { get; set; }
}
