using System.Security.Claims;
using Retaguarda.Shared;
using Retaguarda.Web.Infrastructure;

namespace Retaguarda.UnitTests.Users;

public sealed class ActiveSiteClaimsTests
{
    [Fact]
    public void PreserveFromCurrentPrincipal_copies_active_site_claims_to_refreshed_principal()
    {
        var current = BuildPrincipal(
            new Claim(RetaguardaClaims.SiteId, "7"),
            new Claim(RetaguardaClaims.SiteName, "Planta SP"));
        var refreshed = BuildPrincipal();

        ActiveSiteClaims.PreserveFromCurrentPrincipal(current, refreshed);

        Assert.Equal("7", refreshed.FindFirst(RetaguardaClaims.SiteId)?.Value);
        Assert.Equal("Planta SP", refreshed.FindFirst(RetaguardaClaims.SiteName)?.Value);
    }

    [Fact]
    public void PreserveFromCurrentPrincipal_keeps_existing_refreshed_claims()
    {
        var current = BuildPrincipal(
            new Claim(RetaguardaClaims.SiteId, "7"),
            new Claim(RetaguardaClaims.SiteName, "Planta SP"));
        var refreshed = BuildPrincipal(
            new Claim(RetaguardaClaims.SiteId, "9"),
            new Claim(RetaguardaClaims.SiteName, "Planta RJ"));

        ActiveSiteClaims.PreserveFromCurrentPrincipal(current, refreshed);

        Assert.Equal("9", refreshed.FindFirst(RetaguardaClaims.SiteId)?.Value);
        Assert.Equal("Planta RJ", refreshed.FindFirst(RetaguardaClaims.SiteName)?.Value);
    }

    private static ClaimsPrincipal BuildPrincipal(params Claim[] claims)
    {
        var identity = new ClaimsIdentity(claims, "Identity.Application");
        return new ClaimsPrincipal(identity);
    }
}
