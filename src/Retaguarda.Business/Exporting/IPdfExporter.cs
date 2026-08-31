namespace Retaguarda.Business.Exporting;

// Gera um documento PDF a partir de uma ExportTable. Implementado no Retaguarda.Printing.
public interface IPdfExporter
{
    byte[] Export(ExportTable table);
}
