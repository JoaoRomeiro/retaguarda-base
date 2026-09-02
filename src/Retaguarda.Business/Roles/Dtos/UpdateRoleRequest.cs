namespace Retaguarda.Business.Roles.Dtos;

// Dados de entrada para editar um papel existente. Id é string (PK do Identity).
public sealed class UpdateRoleRequest
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    // Permissões concedidas ao papel (nomes do catálogo, ex.: "sites.edit"). Lista vazia é válida:
    // um papel sem permissão nenhuma existe e simplesmente não dá acesso a nada.
    public List<string> Permissions { get; set; } = [];
}
