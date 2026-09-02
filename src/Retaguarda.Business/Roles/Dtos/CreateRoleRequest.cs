namespace Retaguarda.Business.Roles.Dtos;

// Dados de entrada para criar um papel.
public sealed class CreateRoleRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    // Permissões concedidas ao papel (nomes do catálogo, ex.: "sites.edit"). Lista vazia é válida:
    // um papel sem permissão nenhuma existe e simplesmente não dá acesso a nada.
    public List<string> Permissions { get; set; } = [];
}
