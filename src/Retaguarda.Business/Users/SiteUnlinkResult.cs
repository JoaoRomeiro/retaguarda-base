namespace Retaguarda.Business.Users;

/// <summary>
/// Resultado da tentativa de remover a associação de uma planta com um usuário.
/// </summary>
public enum SiteUnlinkResult
{
    // Associação removida com sucesso.
    Removed,

    // O usuário não existe, ou a planta não está associada a ele.
    NotFound,

    // É a planta padrão do usuário: não pode ser removida (trocar a padrão antes).
    IsDefault,
}
