using Retaguarda.Business.Authentication;
using Retaguarda.Business.Users;
using Retaguarda.Business.Users.Dtos;

namespace Retaguarda.UnitTests.Authentication;

// Plantas selecionáveis do usuário. Nos testes de renovação o que importa é se a planta fixada
// na sessão ainda está na lista — é isso que decide se o refresh sobrevive.
internal sealed class FakeSiteSelectionService : ISiteSelectionService
{
    private readonly IReadOnlyList<AvailableSiteDto> _sites;

    public FakeSiteSelectionService(params int[] siteIds)
        => _sites = [.. siteIds.Select(id => new AvailableSiteDto
        {
            Id = id,
            Code = $"S{id:00}",
            Name = $"Planta {id}",
        })];

    public Task<IReadOnlyList<AvailableSiteDto>> GetSelectableSitesAsync(
        string userId, CancellationToken cancellationToken = default)
        => Task.FromResult(_sites);

    public Task<bool> IsSelectableAsync(string userId, int siteId, CancellationToken cancellationToken = default)
        => Task.FromResult(_sites.Any(s => s.Id == siteId));
}

// Emissor de tokens previsível: os testes de renovação não verificam o conteúdo do JWT (isso é
// do JwtTokenService), só o fluxo em volta dele.
internal sealed class FakeTokenService : ITokenService
{
    public string CreatePreAuthToken(string userId) => $"pre-auth:{userId}";

    public string? ValidatePreAuthToken(string token)
        => token.StartsWith("pre-auth:", StringComparison.Ordinal) ? token["pre-auth:".Length..] : null;

    public AccessTokenResult CreateAccessToken(
        string userId, int siteId, string siteName, IReadOnlyCollection<string> roles)
        => new($"access:{userId}:{siteId}", DateTimeOffset.UtcNow.AddMinutes(30));
}
