using ClosedXML.Excel;
using Retaguarda.Business.Exporting;

namespace Retaguarda.Reporting;

/// <summary>
/// Exportação para Excel (.xlsx) via ClosedXML. Cabeçalho em negrito congelado, auto-filtro e largura
/// ajustada por amostragem (barato para volumes grandes). Genérico — não conhece o domínio.
/// </summary>
public sealed class ClosedXmlExcelExporter : IExcelExporter
{
    // Nº de linhas amostradas para ajustar a largura das colunas (AdjustToContents em tudo é caro
    // para dezenas de milhares de linhas).
    private const int WidthSampleRows = 200;

    // Caracteres inválidos em nome de aba do Excel, além do limite de 31 caracteres.
    private static readonly char[] InvalidSheetChars = ['[', ']', ':', '*', '?', '/', '\\'];

    public byte[] Export(ExportTable table)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add(SheetName(table.Title));

        // Cabeçalho.
        for (var c = 0; c < table.Columns.Count; c++)
        {
            var cell = sheet.Cell(1, c + 1);
            cell.Value = table.Columns[c].Header;
            cell.Style.Font.Bold = true;
            if (table.Columns[c].Align == ExportAlign.Right)
            {
                sheet.Column(c + 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
            }
        }

        // Linhas.
        for (var r = 0; r < table.Rows.Count; r++)
        {
            var row = table.Rows[r];
            for (var c = 0; c < table.Columns.Count; c++)
            {
                // Texto explícito: preserva zeros à esquerda e códigos, e evita a inferência de tipo.
                sheet.Cell(r + 2, c + 1).SetValue(c < row.Count ? row[c] : string.Empty);
            }
        }

        var lastRow = table.Rows.Count + 1;
        var lastCol = table.Columns.Count;

        sheet.SheetView.FreezeRows(1);
        sheet.Range(1, 1, lastRow, lastCol).SetAutoFilter();
        sheet.Columns(1, lastCol).AdjustToContents(1, Math.Min(lastRow, WidthSampleRows));

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    // Nome de aba válido: sem caracteres proibidos e no máximo 31 caracteres.
    private static string SheetName(string title)
    {
        var name = title;
        foreach (var invalid in InvalidSheetChars)
        {
            name = name.Replace(invalid, ' ');
        }

        name = name.Trim();
        if (name.Length > 31)
        {
            name = name[..31];
        }

        return string.IsNullOrWhiteSpace(name) ? "Export" : name;
    }
}
