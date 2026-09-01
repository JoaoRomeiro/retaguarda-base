using System.Net;
using Microsoft.AspNetCore.Http;
using Retaguarda.AspNetCore.Security;

namespace Retaguarda.UnitTests.Security;

// A política em si (janela fixa) é do framework; o que é NOSSO e pode quebrar em silêncio é a
// chave de partição — se ela passar a devolver um valor constante, todos os clientes caem no
// mesmo balde e um único IP consegue bloquear o login de todo mundo.
public sealed class AuthRateLimitingTests
{
    [Fact]
    public void ResolveClientKey_uses_the_client_ip()
    {
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse("203.0.113.10");

        Assert.Equal("203.0.113.10", AuthRateLimiting.ResolveClientKey(context));
    }

    [Fact]
    public void ResolveClientKey_separates_different_ips()
    {
        var first = new DefaultHttpContext();
        first.Connection.RemoteIpAddress = IPAddress.Parse("203.0.113.10");

        var second = new DefaultHttpContext();
        second.Connection.RemoteIpAddress = IPAddress.Parse("203.0.113.11");

        Assert.NotEqual(
            AuthRateLimiting.ResolveClientKey(first),
            AuthRateLimiting.ResolveClientKey(second));
    }

    [Fact]
    public void ResolveClientKey_falls_back_when_ip_is_missing()
    {
        // Sem IP não há como isolar quem é quem: cai num balde único, em vez de ficar sem limite.
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = null;

        Assert.Equal("unknown", AuthRateLimiting.ResolveClientKey(context));
    }

    [Fact]
    public void ResolveClientKey_supports_ipv6()
    {
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse("2001:db8::1");

        Assert.Equal("2001:db8::1", AuthRateLimiting.ResolveClientKey(context));
    }

    [Fact]
    public void Limits_are_sane_for_a_shared_office_ip()
    {
        // Guarda contra alguém "apertar" os limites sem ler o comentário: atrás da Cloudflare o
        // escritório inteiro divide a mesma cota (ver gotcha no CLAUDE.md). Não fixa o valor —
        // só impede que ele caia a um patamar que barraria uso legítimo em horário de pico.
        Assert.True(AuthRateLimiting.CredentialsPermitLimit >= 10);
        Assert.True(AuthRateLimiting.RefreshPermitLimit >= AuthRateLimiting.CredentialsPermitLimit);
        Assert.Equal(TimeSpan.FromMinutes(1), AuthRateLimiting.Window);
    }
}
