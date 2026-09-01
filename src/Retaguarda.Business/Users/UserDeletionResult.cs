namespace Retaguarda.Business.Users;

/// <summary>
/// Resultado da tentativa de exclusão de um usuário. Devolvido para a apresentação
/// exibir a mensagem localizada correta.
/// </summary>
public enum UserDeletionResult
{
    // Excluído logicamente com sucesso.
    Deleted,

    // Usuário não encontrado.
    NotFound,

    // Tentativa de excluir a própria conta (bloqueada).
    SelfDelete,

    // A exclusão deixaria o sistema sem nenhum administrador ativo.
    LastAdmin,
}
