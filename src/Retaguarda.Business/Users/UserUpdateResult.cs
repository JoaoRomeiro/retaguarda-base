namespace Retaguarda.Business.Users;

/// <summary>
/// Resultado da tentativa de atualizar um usuário. Devolvido para a apresentação exibir a
/// mensagem localizada correta — as recusas abaixo NÃO são erro de validação de entrada:
/// os dados estão bem formados, a operação é que deixaria o sistema inconsistente.
/// </summary>
public enum UserUpdateResult
{
    // Atualizado com sucesso.
    Updated,

    // Usuário não encontrado.
    NotFound,

    // Tentativa de desativar a própria conta (o autor perderia o acesso na hora).
    SelfDeactivate,

    // Tentativa de trocar o próprio papel (o autor poderia se rebaixar sem volta).
    SelfRoleChange,

    // A operação deixaria o sistema sem nenhum administrador ativo.
    LastAdmin,
}
