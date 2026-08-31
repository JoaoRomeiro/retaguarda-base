using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Retaguarda.AspNetCore.Security;

namespace Retaguarda.UnitTests.Security;

// Testes do middleware de headers de segurança (Retaguarda.AspNetCore).
// Valida que cada resposta recebe os headers esperados e que o pipeline continua.
public class SecurityHeadersMiddlewareTests
{
    // Executa o middleware sobre um HttpContext novo e devolve o contexto + se o next foi chamado.
    private static async Task<(HttpContext Context, bool NextCalled)> RunAsync(SecurityHeadersOptions options)
    {
        var context = new DefaultHttpContext();
        var nextCalled = false;
        RequestDelegate next = _ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        };

        var middleware = new SecurityHeadersMiddleware(next, options);
        await middleware.InvokeAsync(context);

        return (context, nextCalled);
    }

    [Fact]
    public async Task InvokeAsync_DefineHeadersFixosDeSeguranca()
    {
        var (context, _) = await RunAsync(new SecurityHeadersOptions());

        Assert.Equal("nosniff", context.Response.Headers["X-Content-Type-Options"].ToString());
        Assert.Equal("DENY", context.Response.Headers["X-Frame-Options"].ToString());
    }

    [Fact]
    public async Task InvokeAsync_UsaContentSecurityPolicyConfigurada()
    {
        var options = new SecurityHeadersOptions { ContentSecurityPolicy = "default-src 'none'" };

        var (context, _) = await RunAsync(options);

        Assert.Equal("default-src 'none'", context.Response.Headers["Content-Security-Policy"].ToString());
    }

    [Fact]
    public async Task InvokeAsync_UsaReferrerPolicyConfigurada()
    {
        var options = new SecurityHeadersOptions { ReferrerPolicy = "strict-origin" };

        var (context, _) = await RunAsync(options);

        Assert.Equal("strict-origin", context.Response.Headers["Referrer-Policy"].ToString());
    }

    [Fact]
    public async Task InvokeAsync_ChamaProximoMiddleware()
    {
        var (_, nextCalled) = await RunAsync(new SecurityHeadersOptions());

        Assert.True(nextCalled);
    }

    [Fact]
    public void Options_TemDefaultsSeguros()
    {
        var options = new SecurityHeadersOptions();

        Assert.Equal("default-src 'self'", options.ContentSecurityPolicy);
        Assert.Equal("no-referrer", options.ReferrerPolicy);
    }

    [Fact]
    public void UseSecurityHeaders_LancaQuandoConfigureForNulo()
    {
        var services = new ServiceCollection().BuildServiceProvider();
        IApplicationBuilder app = new ApplicationBuilder(services);

        Assert.Throws<ArgumentNullException>(() => app.UseSecurityHeaders(null!));
    }
}
