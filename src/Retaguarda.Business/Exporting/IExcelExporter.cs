namespace Retaguarda.Business.Exporting;

// Gera uma planilha Excel (.xlsx) a partir de uma ExportTable. Implementado no Retaguarda.Reporting.
public interface IExcelExporter
{
    byte[] Export(ExportTable table);
}
