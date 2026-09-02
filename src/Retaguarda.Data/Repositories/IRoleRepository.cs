using Retaguarda.Data.Identity;

namespace Retaguarda.Data.Repositories;

/// <summary>
/// Acesso a dados de <see cref="ApplicationRole"/> (papéis do Identity estendidos).
/// Definido aqui (e não em Shared/Business) porque é tipado na entidade; assim Data
/// implementa sem referenciar Business (§4.2). Escopo global: papéis não têm SiteId.
/// </summary>
public interface IRoleRepository
{
    Task<ApplicationRole?> GetByIdAsync(string id, CancellationToken cancellationToken = default);

    // Listagem paginada com busca opcional (Name/Description).
    Task<(IReadOnlyList<ApplicationRole> Items, int TotalCount)> ListAsync(
        string? search, int page, int pageSize, CancellationToken cancellationToken = default);

    // True se já existe um papel com este nome (compara NormalizedName), opcionalmente
    // excluindo um Id na edição. Considera apenas papéis não-excluídos.
    Task<bool> NameExistsAsync(string name, string? excludeId, CancellationToken cancellationToken = default);

    // Quantos usuários estão vinculados ao papel (guarda de exclusão).
    Task<int> CountUsersInRoleAsync(string roleId, CancellationToken cancellationToken = default);

    // Permissões concedidas ao papel, lidas das claims do tipo RetaguardaClaims.Permission
    // (identity."RoleClaims"). Outras claims do papel, se existirem, não são tocadas.
    Task<IReadOnlyList<string>> GetPermissionsAsync(string roleId, CancellationToken cancellationToken = default);

    // Substitui o conjunto de permissões do papel pelo informado (concede o que falta, revoga o
    // que sobra). Idempotente.
    Task SetPermissionsAsync(
        ApplicationRole role, IReadOnlyCollection<string> permissions, CancellationToken cancellationToken = default);

    // Create/Update vão pelo RoleManager (mantém NormalizedName/ConcurrencyStamp).
    Task<ApplicationRole> AddAsync(ApplicationRole role, CancellationToken cancellationToken = default);
    Task UpdateAsync(ApplicationRole role, CancellationToken cancellationToken = default);

    // Exclusão lógica: o interceptor converte o Remove em soft delete (não usa RoleManager.DeleteAsync).
    Task DeleteAsync(ApplicationRole role, CancellationToken cancellationToken = default);
}
