namespace Retaguarda.Shared.Models;

/// <summary>
/// Página de resultados de uma listagem (busca paginada). Tipo neutro, usado
/// por Business (saída dos serviços) e Web/Api (renderização/serialização).
/// </summary>
public sealed class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; }
    public int TotalCount { get; }
    public int Page { get; }
    public int PageSize { get; }

    // Total de páginas dado o tamanho de página atual.
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);

    public PagedResult(IReadOnlyList<T> items, int totalCount, int page, int pageSize)
    {
        Items = items;
        TotalCount = totalCount;
        Page = page;
        PageSize = pageSize;
    }
}
