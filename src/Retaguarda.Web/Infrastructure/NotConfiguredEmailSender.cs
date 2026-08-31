using Retaguarda.Business.Notifications;

namespace Retaguarda.Web.Infrastructure;

/// <summary>
/// Placeholder de <see cref="IEmailSender"/> para ambientes != Development enquanto
/// o envio SMTP (MailKit) não é implementado. Não envia nada e falha de forma
/// explícita se for chamado — evita "perder" e-mails de recuperação silenciosamente.
/// Substituir pelo sender SMTP em etapa dedicada. A falha no construtor não ocorre:
/// só lança ao tentar enviar, então o restante do AccountController (login) segue ok.
/// </summary>
public sealed class NotConfiguredEmailSender : IEmailSender
{
    public Task SendEmailAsync(
        string to,
        string subject,
        string htmlBody,
        CancellationToken cancellationToken = default)
        => throw new InvalidOperationException(
            "Email sending is not configured. The SMTP (MailKit) sender is pending implementation.");
}
