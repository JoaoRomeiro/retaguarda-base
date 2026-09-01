using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Retaguarda.AspNetCore.Security;

// Opções de configuração do middleware de segurança.
// CSP varia entre Web (precisa de 'unsafe-inline' em style-src por causa do Bootstrap)
// e Api (default-src 'none'; resposta JSON não pede recurso algum).
// Headers fixos (X-Content-Type-Options, X-Frame-Options) são iguais para todos.
public sealed class SecurityHeadersOptions
{
    /// <summary>
    /// Valor completo do header Content-Security-Policy.
    /// Configurado por projeto — ver §15.10 do doc de orientação.
    /// </summary>
    public string ContentSecurityPolicy { get; set; } = "default-src 'self'";

    /// <summary>
    /// Valor do header Referrer-Policy. Default 'no-referrer' (sistema interno).
    /// </summary>
    public string ReferrerPolicy { get; set; } = "no-referrer";

    /// <summary>
    /// Valor do header Permissions-Policy. Default desliga câmera, microfone e geolocalização:
    /// nada na plataforma usa esses recursos, e negar por padrão vale também para o que for
    /// embutido numa página (iframe, script de terceiro).
    /// </summary>
    public string PermissionsPolicy { get; set; } = "camera=(), microphone=(), geolocation=()";
}

// Middleware que aplica os headers de segurança a TODAS as respostas.
// Posicionar como primeiro middleware da pipeline para cobrir arquivos estáticos,
// redirects e respostas de erro.
public sealed class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;
    private readonly SecurityHeadersOptions _options;

    public SecurityHeadersMiddleware(RequestDelegate next, SecurityHeadersOptions options)
    {
        _next = next;
        _options = options;
    }

    public Task InvokeAsync(HttpContext context)
    {
        var headers = context.Response.Headers;

        // CSP — política de origens permitidas.
        headers["Content-Security-Policy"] = _options.ContentSecurityPolicy;

        // Browser não tenta adivinhar MIME type (mitiga XSS via uploads).
        headers["X-Content-Type-Options"] = "nosniff";

        // Anti-clickjacking — legacy de frame-ancestors 'none', mantido por compatibilidade.
        headers["X-Frame-Options"] = "DENY";

        // Política de Referer — não propagar URL interna em navegação externa.
        headers["Referrer-Policy"] = _options.ReferrerPolicy;

        // Recursos do navegador negados por padrão (câmera, microfone, geolocalização).
        headers["Permissions-Policy"] = _options.PermissionsPolicy;

        return _next(context);
    }
}

public static class SecurityHeadersMiddlewareExtensions
{
    /// <summary>
    /// Adiciona o middleware de headers de segurança à pipeline.
    /// Chamar como PRIMEIRO middleware para cobrir todas as respostas.
    /// </summary>
    public static IApplicationBuilder UseSecurityHeaders(
        this IApplicationBuilder app,
        Action<SecurityHeadersOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        var options = new SecurityHeadersOptions();
        configure(options);
        return app.UseMiddleware<SecurityHeadersMiddleware>(options);
    }
}
