namespace Retaguarda.Business.Notifications;

/// <summary>
/// Abstração de envio de e-mail (recuperação de senha, notificações).
/// A implementação de produção (SMTP, via MailKit) entra em etapa dedicada de cada projeto; em
/// Development usa-se um sender que registra a mensagem no log.
/// </summary>
public interface IEmailSender
{
    /// <summary>
    /// Envia um e-mail. O corpo já chega renderizado (texto/HTML simples) —
    /// a montagem a partir dos templates .resx é responsabilidade de quem chama.
    /// </summary>
    Task SendEmailAsync(
        string to,
        string subject,
        string htmlBody,
        CancellationToken cancellationToken = default);
}
