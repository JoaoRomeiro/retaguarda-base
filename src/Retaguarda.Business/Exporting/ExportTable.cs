namespace Retaguarda.Business.Exporting;

// Alinhamento de uma coluna na exportação (números à direita).
public enum ExportAlign
{
    Left,
    Right,
}

// Uma coluna da tabela exportada: cabeçalho + alinhamento.
public sealed record ExportColumn(string Header, ExportAlign Align = ExportAlign.Left);

/// <summary>
/// Representação neutra de uma tabela a exportar (Excel/PDF), sem conhecer o domínio. A camada Web
/// monta a partir dos DTOs (cabeçalhos localizados, datas no fuso de exibição) e passa aos
/// exportadores. Reutilizável por qualquer tela — ver SitesController.Export como referência.
/// </summary>
/// <param name="Title">Título do relatório (também vira o nome da aba no Excel).</param>
/// <param name="MetaLines">Linhas de contexto (planta, filtros, gerado em) — exibidas no cabeçalho do PDF.</param>
/// <param name="Columns">Definição das colunas.</param>
/// <param name="Rows">Linhas já formatadas como texto (uma lista de células por linha).</param>
public sealed record ExportTable(
    string Title,
    IReadOnlyList<string> MetaLines,
    IReadOnlyList<ExportColumn> Columns,
    IReadOnlyList<IReadOnlyList<string>> Rows);
