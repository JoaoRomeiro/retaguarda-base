using Retaguarda.Data.Identity;

namespace Retaguarda.Data.Repositories;

/// <summary>
/// Acesso a dados de <see cref="ApplicationUser"/> (usuários do Identity estendidos).
/// Orquestra UserManager + DbContext. Um usuário tem exatamente uma Role e 1..N plantas
/// (UserSite), com uma planta padrão (DefaultSiteId).
/// </summary>
public interface IUserRepository
{
    // Inclui DefaultSite; NÃO rastreia SiteLinks (use GetSiteIdsAsync) para evitar conflito no update.
    Task<ApplicationUser?> GetByIdAsync(string id, CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<ApplicationUser> Items, int TotalCount)> ListAsync(
        string? search, int page, int pageSize, CancellationToken cancellationToken = default);

    // Plantas vinculadas a um usuário.
    Task<IReadOnlyList<int>> GetSiteIdsAsync(string userId, CancellationToken cancellationToken = default);

    // Nome da (única) role de cada usuário, para a listagem.
    Task<IReadOnlyDictionary<string, string?>> GetRoleNamesAsync(
        IReadOnlyCollection<string> userIds, CancellationToken cancellationToken = default);

    Task<string?> GetRoleNameAsync(string userId, CancellationToken cancellationToken = default);

    // Checagens de unicidade/existência usadas pelos validators.
    Task<bool> EmailExistsAsync(string email, string? excludeId, CancellationToken cancellationToken = default);
    Task<bool> RoleExistsAsync(string roleName, CancellationToken cancellationToken = default);
    Task<bool> SitesExistAsync(IReadOnlyCollection<int> siteIds, CancellationToken cancellationToken = default);

    // True se a planta já está associada ao usuário (valida a planta padrão na edição).
    Task<bool> IsSiteLinkedAsync(string userId, int siteId, CancellationToken cancellationToken = default);

    // --- Sub-CRUD "Plantas do usuário" (associação N:N) ---

    // Plantas associadas ao usuário (paginadas, com busca por código/nome).
    Task<(IReadOnlyList<Entities.Site> Items, int TotalCount)> ListLinkedSitesAsync(
        string userId, string? search, int page, int pageSize, CancellationToken cancellationToken = default);

    // Plantas ativas ainda não associadas ao usuário (opções para associar).
    Task<IReadOnlyList<Entities.Site>> GetAvailableSitesAsync(string userId, CancellationToken cancellationToken = default);

    // Plantas ativas E associadas ao usuário (opções da seleção de planta no login — roadmap 2.2.1).
    Task<IReadOnlyList<Entities.Site>> GetActiveLinkedSitesAsync(string userId, CancellationToken cancellationToken = default);

    // True se a planta existe, está ativa e ainda não está associada ao usuário.
    Task<bool> IsSiteAvailableForUserAsync(string userId, int siteId, CancellationToken cancellationToken = default);

    Task AddSiteLinkAsync(string userId, int siteId, CancellationToken cancellationToken = default);
    Task RemoveSiteLinkAsync(string userId, int siteId, CancellationToken cancellationToken = default);

    // Cria o usuário com senha, vincula a role única e as plantas iniciais.
    Task<ApplicationUser> AddAsync(
        ApplicationUser user, string password, string roleName, IReadOnlyCollection<int> siteIds,
        CancellationToken cancellationToken = default);

    // Atualiza o perfil e sincroniza a role única. NÃO altera as plantas associadas
    // (isso é responsabilidade do sub-CRUD de Plantas do usuário).
    Task UpdateAsync(
        ApplicationUser user, string roleName, CancellationToken cancellationToken = default);

    // Exclusão lógica (o interceptor converte o Remove em soft delete).
    Task DeleteAsync(ApplicationUser user, CancellationToken cancellationToken = default);
}
