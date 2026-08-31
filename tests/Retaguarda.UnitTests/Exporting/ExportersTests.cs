using Retaguarda.Business.Exporting;
using Retaguarda.Printing;
using Retaguarda.Reporting;

namespace Retaguarda.UnitTests.Exporting;

// Testes dos exportadores: garantem que produzem um arquivo do formato certo (assinatura de bytes)
// a partir de uma ExportTable. O conteúdo visual é validado manualmente/em runtime.
public sealed class ExportersTests
{
    private static ExportTable SampleTable() => new(
        Title: "Plantas",
        MetaLines: ["Planta: DEV", "Gerado em: 28/07/2026 10:00:00"],
        Columns:
        [
            new ExportColumn("Data/hora"),
            new ExportColumn("Código"),
            new ExportColumn("RSSI", ExportAlign.Right),
        ],
        Rows:
        [
            ["28/07/2026 10:00:00", "E200341101", "-65"],
            ["28/07/2026 10:00:01", "E200341102", "-71"],
        ]);

    [Fact]
    public void Excel_export_produces_a_valid_xlsx_signature()
    {
        var bytes = new ClosedXmlExcelExporter().Export(SampleTable());

        Assert.NotEmpty(bytes);
        // XLSX é um ZIP: começa com "PK\x03\x04".
        Assert.True(bytes.Length > 4 && bytes[0] == 0x50 && bytes[1] == 0x4B && bytes[2] == 0x03 && bytes[3] == 0x04);
    }

    [Fact]
    public void Pdf_export_produces_a_valid_pdf_signature()
    {
        var bytes = new QuestPdfExporter().Export(SampleTable());

        Assert.NotEmpty(bytes);
        // PDF começa com "%PDF".
        Assert.True(bytes.Length > 4 && bytes[0] == 0x25 && bytes[1] == 0x50 && bytes[2] == 0x44 && bytes[3] == 0x46);
    }

    [Fact]
    public void Excel_export_handles_empty_rows()
    {
        var table = new ExportTable("Vazio", [], [new ExportColumn("Col")], []);

        var bytes = new ClosedXmlExcelExporter().Export(table);

        Assert.NotEmpty(bytes);
    }
}
