using Retaguarda.Data.Entities;

namespace Retaguarda.Data.Repositories;

/// <summary>
/// Acesso a dados de <see cref="Site"/>. Definido aqui (e não em Shared/Business)
/// porque é tipado na entidade; assim Data implementa sem referenciar Business (§4.2).
/// </summary>
public interface ISiteRepository
{
    Task<Site?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    // Listagem paginada com busca opcional (Name/Code).
    Task<(IReadOnlyList<Site> Items, int TotalCount)> ListAsync(
        string? search, int page, int pageSize, CancellationToken cancellationToken = default);

    // True se já existe um site com este Code (opcionalmente excluindo um Id na edição).
    Task<bool> CodeExistsAsync(string code, int? excludeId, CancellationToken cancellationToken = default);

    // True se já existe um site com este Name (opcionalmente excluindo um Id na edição).
    Task<bool> NameExistsAsync(string name, int? excludeId, CancellationToken cancellationToken = default);

    Task<Site> AddAsync(Site site, CancellationToken cancellationToken = default);
    Task UpdateAsync(Site site, CancellationToken cancellationToken = default);

    // Exclusão lógica: o interceptor converte o Remove em soft delete.
    Task DeleteAsync(Site site, CancellationToken cancellationToken = default);
}
