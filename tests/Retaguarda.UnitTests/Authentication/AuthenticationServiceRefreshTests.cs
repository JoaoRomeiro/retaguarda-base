using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Retaguarda.Business.Authentication;
using Retaguarda.Business.Authentication.Dtos;
using Retaguarda.Business.Users;
using Retaguarda.Data.Identity;
using Retaguarda.Shared.Authentication;
using Retaguarda.UnitTests.Users;

namespace Retaguarda.UnitTests.Authentication;

// Primeiros testes do fluxo de autenticação da Api. Cobrem a RENOVAÇÃO de token — em especial a
// detecção de reuso, que decide revogar a sessão inteira e por isso não pode disparar por engano.
public sealed class AuthenticationServiceRefreshTests
{
    private const string UserId = "user-1";

    // O serviço guarda o hash SHA-256 (Base64) do token, nunca o valor bruto.
    private static string HashOf(string raw)
        => Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(raw)));

    private static UserManager<ApplicationUser> BuildUserManager(FakeUserStore store)
        => new(
            store,
            Options.Create(new IdentityOptions()),
            new PasswordHasher<ApplicationUser>(),
            userValidators: [],
            passwordValidators: [],
            new UpperInvariantLookupNormalizer(),
            new IdentityErrorDescriber(),
            services: null!,
            NullLogger<UserManager<ApplicationUser>>.Instance);

    private static AuthenticationService BuildService(
        FakeUserStore store, FakeRefreshTokenRepository refreshTokens, FakeSiteSelectionService sites)
        => new(
            BuildUserManager(store),
            sites,
            new FakeTokenService(),
            refreshTokens,
            Options.Create(new JwtOptions { SigningKey = new string('k', 32) }),
            NullLogger<AuthenticationService>.Instance);

    [Fact]
    public async Task RefreshAsync_rotates_an_active_token()
    {
        var store = new FakeUserStore();
        store.Add(UserId, isActive: true, "Admin");
        var tokens = new FakeRefreshTokenRepository();
        tokens.SeedActive(UserId, HashOf("token-vivo"));
        var service = BuildService(store, tokens, new FakeSiteSelectionService(1));

        var outcome = await service.RefreshAsync("token-vivo");

        Assert.Equal(RefreshStatus.Success, outcome.Status);
        // Rotação: o usado é revogado e um novo par é emitido.
        Assert.NotNull(tokens.Tokens[0].RevokedAt);
        Assert.Equal(2, tokens.Tokens.Count);
        Assert.Null(tokens.Tokens[1].RevokedAt);
        // Caminho feliz não pode acionar a revogação em massa.
        Assert.Equal(0, tokens.RevokeAllCalls);
    }

    [Fact]
    public async Task RefreshAsync_revokes_the_whole_session_when_a_revoked_token_is_reused()
    {
        var store = new FakeUserStore();
        store.Add(UserId, isActive: true, "Admin");
        var tokens = new FakeRefreshTokenRepository();
        tokens.SeedRevoked(UserId, HashOf("token-vazado"));   // já rotacionado
        tokens.SeedActive(UserId, HashOf("token-atual"));     // a sessão viva
        var service = BuildService(store, tokens, new FakeSiteSelectionService(1));

        var outcome = await service.RefreshAsync("token-vazado");

        Assert.Equal(RefreshStatus.Invalid, outcome.Status);
        // Não dá para saber quem é o dono: derruba a sessão inteira e força login novo.
        Assert.Equal(1, tokens.RevokeAllCalls);
        Assert.NotNull(tokens.Tokens[1].RevokedAt);
    }

    [Fact]
    public async Task RefreshAsync_does_not_treat_an_expired_token_as_reuse()
    {
        var store = new FakeUserStore();
        store.Add(UserId, isActive: true, "Admin");
        var tokens = new FakeRefreshTokenRepository();
        tokens.SeedExpired(UserId, HashOf("token-vencido"));  // venceu, nunca foi revogado
        tokens.SeedActive(UserId, HashOf("token-atual"));
        var service = BuildService(store, tokens, new FakeSiteSelectionService(1));

        var outcome = await service.RefreshAsync("token-vencido");

        // Validade vencida é uso normal de um cliente que ficou offline — não é sinal de roubo.
        // Derrubar a sessão aqui deslogaria gente legítima sem motivo.
        Assert.Equal(RefreshStatus.Invalid, outcome.Status);
        Assert.Equal(0, tokens.RevokeAllCalls);
        Assert.Null(tokens.Tokens[1].RevokedAt);
    }

    [Fact]
    public async Task RefreshAsync_ignores_an_unknown_token()
    {
        var store = new FakeUserStore();
        store.Add(UserId, isActive: true, "Admin");
        var tokens = new FakeRefreshTokenRepository();
        var service = BuildService(store, tokens, new FakeSiteSelectionService(1));

        var outcome = await service.RefreshAsync("nunca-existiu");

        // Hash desconhecido: chute ou lixo. Nada a revogar.
        Assert.Equal(RefreshStatus.Invalid, outcome.Status);
        Assert.Equal(0, tokens.RevokeAllCalls);
    }

    [Fact]
    public async Task RefreshAsync_refuses_an_inactive_user()
    {
        var store = new FakeUserStore();
        store.Add(UserId, isActive: false, "Admin");
        var tokens = new FakeRefreshTokenRepository();
        tokens.SeedActive(UserId, HashOf("token-vivo"));
        var service = BuildService(store, tokens, new FakeSiteSelectionService(1));

        var outcome = await service.RefreshAsync("token-vivo");

        Assert.Equal(RefreshStatus.Invalid, outcome.Status);
    }

    [Fact]
    public async Task RefreshAsync_refuses_when_the_user_lost_access_to_the_session_site()
    {
        var store = new FakeUserStore();
        store.Add(UserId, isActive: true, "Admin");
        var tokens = new FakeRefreshTokenRepository();
        tokens.SeedActive(UserId, HashOf("token-vivo"), siteId: 1);
        // O vínculo com a planta 1 sumiu (ou a planta foi desativada): renovar exige login novo.
        var service = BuildService(store, tokens, new FakeSiteSelectionService(99));

        var outcome = await service.RefreshAsync("token-vivo");

        Assert.Equal(RefreshStatus.Invalid, outcome.Status);
    }
}
