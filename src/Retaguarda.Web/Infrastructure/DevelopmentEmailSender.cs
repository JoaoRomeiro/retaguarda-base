using Retaguarda.Business.Notifications;

namespace Retaguarda.Web.Infrastructure;

/// <summary>
/// Implementação de <see cref="IEmailSender"/> para Development: não envia e-mail
/// de verdade, apenas registra destinatário, assunto e corpo no log — permitindo
/// copiar o link de recuperação e testar o fluxo sem servidor SMTP.
/// Registrada SOMENTE em Development (ver Program.cs). NUNCA usar em produção:
/// o sender SMTP de produção não loga o corpo, para não expor tokens.
/// </summary>
public sealed class DevelopmentEmailSender : IEmailSender
{
    private readonly ILogger<DevelopmentEmailSender> _logger;

    public DevelopmentEmailSender(ILogger<DevelopmentEmailSender> logger) => _logger = logger;

    public Task SendEmailAsync(
        string to,
        string subject,
        string htmlBody,
        CancellationToken cancellationToken = default)
    {
        // Dev-only: o corpo traz o link de reset; logado para facilitar o teste local.
        _logger.LogInformation(
            "DEV email (not sent over SMTP). To: {EmailTo} | Subject: {EmailSubject} | Body: {EmailBody}",
            to, subject, htmlBody);
        return Task.CompletedTask;
    }
}
