using System.Globalization;
using System.Threading.RateLimiting;
// AddRateLimiter é declarado em Microsoft.AspNetCore.Builder (não em ...DependencyInjection,
// como a maioria dos Add*): sem este using o método não é encontrado.
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;

namespace Retaguarda.AspNetCore.Security;

/// <summary>
/// Rate limiting dos endpoints ANÔNIMOS de autenticação, compartilhado por Web e Api.
/// Janela fixa particionada por IP do cliente, aplicada endpoint a endpoint via
/// <c>[EnableRateLimiting]</c> — nunca global, para que <c>/health</c>, arquivos estáticos e as
/// telas autenticadas fiquem de fora sem precisar de lista de exceções.
/// </summary>
/// <remarks>
/// <para>
/// PARTIÇÃO POR IP — LEIA ANTES DE MUDAR OS LIMITES.
/// A cota é por endereço IP. Atrás da Cloudflare + Caddy (ver docs/deploy.md), TODOS os usuários
/// de um mesmo escritório chegam com o MESMO IP público, e portanto dividem a mesma cota. Por isso
/// os limites não são apertados: o número precisa acomodar o pico legítimo (início de expediente,
/// vários logins no mesmo minuto) sem deixar de barrar volume de ataque.
/// </para>
/// <para>
/// Sintoma de limite baixo demais: usuário legítimo recebe 429 no login em horário de pico.
/// Nesse caso, aumente <see cref="CredentialsPermitLimit"/> — está registrado como gotcha no
/// CLAUDE.md. Ponto de partida escolhido sem dado real de uso; reavaliar com telemetria.
/// </para>
/// <para>
/// O que isto protege: volume — password spraying (muitas contas, uma senha cada, que o lockout
/// por conta não pega), spam de e-mail de recuperação e consumo de CPU no hash de senha.
/// O que NÃO substitui: o lockout do Identity, que continua sendo a proteção POR CONTA.
/// São camadas diferentes.
/// </para>
/// <para>
/// O IP só é confiável enquanto os apps forem acessíveis apenas pelo proxy reverso: a pipeline
/// aceita <c>X-Forwarded-For</c> de qualquer origem (KnownProxies limpo, ver Program.cs). Publicar
/// a porta do app direto na internet torna o cabeçalho falsificável e este limitador, contornável.
/// </para>
/// </remarks>
public static class AuthRateLimiting
{
    /// <summary>Endpoints que recebem credencial ou disparam e-mail: login, seleção de planta, esqueci/redefinir senha.</summary>
    public const string CredentialsPolicy = "auth-credentials";

    /// <summary>Renovação de token da Api: chamada de rotina de cliente já autenticado, tolerância maior.</summary>
    public const string RefreshPolicy = "auth-refresh";

    /// <summary>Requisições por janela, por IP, nos endpoints de credencial. Ver o comentário da classe antes de baixar.</summary>
    public const int CredentialsPermitLimit = 20;

    /// <summary>Requisições por janela, por IP, na renovação de token.</summary>
    public const int RefreshPermitLimit = 60;

    /// <summary>Janela das duas políticas.</summary>
    public static readonly TimeSpan Window = TimeSpan.FromMinutes(1);

    // Partição quando o IP não está disponível (ex.: chamada em processo, teste sem conexão).
    // Todos caem no mesmo balde de propósito: sem IP não há como isolar quem é quem.
    private const string UnknownClientKey = "unknown";

    /// <summary>
    /// Registra as políticas de autenticação. O host define o <c>OnRejected</c>, porque o corpo
    /// da resposta 429 difere: ProblemDetails na Api, página no navegador (Web).
    /// </summary>
    public static IServiceCollection AddAuthRateLimiter(
        this IServiceCollection services,
        Func<OnRejectedContext, CancellationToken, ValueTask> onRejected)
    {
        ArgumentNullException.ThrowIfNull(onRejected);

        return services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = onRejected;

            options.AddPolicy(CredentialsPolicy, httpContext =>
                FixedWindowByClient(httpContext, CredentialsPermitLimit));

            options.AddPolicy(RefreshPolicy, httpContext =>
                FixedWindowByClient(httpContext, RefreshPermitLimit));
        });
    }

    /// <summary>
    /// Chave de partição da requisição: o IP do cliente, ou <c>unknown</c> quando não houver.
    /// Público para ser testável sem subir um host.
    /// </summary>
    public static string ResolveClientKey(HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        // IP real: UseForwardedHeaders roda antes e já aplicou o X-Forwarded-For do proxy.
        return httpContext.Connection.RemoteIpAddress?.ToString() ?? UnknownClientKey;
    }

    /// <summary>
    /// Preenche <c>Retry-After</c> (em segundos) a partir do lease recusado, quando o limitador
    /// souber informar. RFC 9110: diz ao cliente quando vale a pena tentar de novo.
    /// </summary>
    public static void ApplyRetryAfter(OnRejectedContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            context.HttpContext.Response.Headers.RetryAfter =
                ((int)retryAfter.TotalSeconds).ToString(NumberFormatInfo.InvariantInfo);
        }
    }

    private static RateLimitPartition<string> FixedWindowByClient(HttpContext httpContext, int permitLimit)
        => RateLimitPartition.GetFixedWindowLimiter(
            ResolveClientKey(httpContext),
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = permitLimit,
                Window = Window,
                // Sem fila: excedeu, recusa na hora. Enfileirar login só empurra a latência para
                // o usuário legítimo e segura recurso do servidor durante um ataque.
                QueueLimit = 0,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            });
}
