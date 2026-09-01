using System.Globalization;
using System.Text.Encodings.Web;

namespace Retaguarda.Web.Infrastructure;

/// <summary>
/// Página HTML mínima da resposta 429 (rate limiting dos endpoints anônimos de autenticação).
/// Não é uma view Razor de propósito: o OnRejected do limitador corta a pipeline antes do MVC,
/// e reexecutar o pipeline só para desenhar uma tela de erro não compensa. Reaproveita o CSS da
/// aplicação, então herda o tema; se o CSS não carregar, o texto continua legível.
/// Os textos chegam já localizados (.resx) e são escapados aqui.
/// </summary>
internal static class TooManyRequestsPage
{
    public static string Render(string title, string message)
    {
        var encodedTitle = HtmlEncoder.Default.Encode(title);
        var encodedMessage = HtmlEncoder.Default.Encode(message);
        var lang = HtmlEncoder.Default.Encode(CultureInfo.CurrentUICulture.Name);

        return $"""
            <!DOCTYPE html>
            <html lang="{lang}">
            <head>
                <meta charset="utf-8" />
                <meta name="viewport" content="width=device-width, initial-scale=1.0" />
                <title>{encodedTitle}</title>
                <link rel="stylesheet" href="/lib/bootstrap/dist/css/bootstrap.min.css" />
                <link rel="stylesheet" href="/css/theme.css" />
                <link rel="stylesheet" href="/css/site.css" />
            </head>
            <body>
                <main class="auth-shell">
                    <div class="auth-card">
                        <h1 class="h5 mb-3 text-center">{encodedTitle}</h1>
                        <p class="text-center mb-0">{encodedMessage}</p>
                    </div>
                </main>
            </body>
            </html>
            """;
    }
}
