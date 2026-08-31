namespace Retaguarda.Shared;

// Tipo marcador (sem código). Serve como T em IStringLocalizer<Emails> para resolver
// os templates de e-mail em Resources/Emails.<culture>.resx (ver §12.5 do doc).
// Separado de SharedResources porque e-mails têm ciclo de tradução próprio.
public sealed class Emails
{
}
