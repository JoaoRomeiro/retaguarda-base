using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using RetaguardaExport = Retaguarda.Business.Exporting;

namespace Retaguarda.Printing;

/// <summary>
/// Exportação para PDF via QuestPDF (licença Community). Página A4 paisagem: cabeçalho com título +
/// linhas de contexto, tabela com as colunas da ExportTable e rodapé com paginação. Genérico —
/// não conhece o domínio.
/// </summary>
public sealed class QuestPdfExporter : RetaguardaExport.IPdfExporter
{
    static QuestPdfExporter()
    {
        // Licença Community (grátis para uso individual/empresas pequenas). Setada uma única vez.
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public byte[] Export(RetaguardaExport.ExportTable table)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(20);
                page.DefaultTextStyle(t => t.FontSize(9));

                page.Header().Column(column =>
                {
                    column.Item().Text(table.Title).SemiBold().FontSize(14);
                    foreach (var line in table.MetaLines)
                    {
                        column.Item().Text(line).FontSize(8).FontColor(Colors.Grey.Darken1);
                    }
                });

                page.Content().PaddingVertical(6).Table(t =>
                {
                    t.ColumnsDefinition(columns =>
                    {
                        foreach (var _ in table.Columns)
                        {
                            columns.RelativeColumn();
                        }
                    });

                    t.Header(header =>
                    {
                        foreach (var col in table.Columns)
                        {
                            var container = header.Cell()
                                .BorderBottom(1).BorderColor(Colors.Grey.Medium)
                                .PaddingVertical(3).PaddingHorizontal(2);

                            if (col.Align == RetaguardaExport.ExportAlign.Right)
                            {
                                container = container.AlignRight();
                            }

                            container.Text(col.Header).SemiBold().FontSize(8);
                        }
                    });

                    foreach (var row in table.Rows)
                    {
                        for (var c = 0; c < table.Columns.Count; c++)
                        {
                            var value = c < row.Count ? row[c] : string.Empty;
                            var container = t.Cell()
                                .BorderBottom(1).BorderColor(Colors.Grey.Lighten2)
                                .PaddingVertical(2).PaddingHorizontal(2);

                            if (table.Columns[c].Align == RetaguardaExport.ExportAlign.Right)
                            {
                                container = container.AlignRight();
                            }

                            container.Text(value).FontSize(8);
                        }
                    }
                });

                page.Footer().AlignRight().Text(text =>
                {
                    text.CurrentPageNumber();
                    text.Span(" / ");
                    text.TotalPages();
                });
            });
        }).GeneratePdf();
    }
}
