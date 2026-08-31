namespace Retaguarda.Business.Roles;

/// <summary>
/// Resultado da tentativa de exclusão de um papel. A exclusão de papéis tem guardas
/// específicas (papel interno e papel em uso), então o motivo do bloqueio é devolvido
/// para a camada de apresentação exibir a mensagem localizada correta.
/// </summary>
public enum RoleDeletionResult
{
    // Excluído logicamente com sucesso.
    Deleted,

    // Papel não encontrado.
    NotFound,

    // Papel interno (IsSystem): protegido contra exclusão.
    SystemRole,

    // Há usuários vinculados ao papel.
    HasAssignedUsers,
}
