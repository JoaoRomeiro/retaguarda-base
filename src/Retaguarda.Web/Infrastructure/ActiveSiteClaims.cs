using System.Security.Claims;
using Retaguarda.Shared;

namespace Retaguarda.Web.Infrastructure;

public static class ActiveSiteClaims
{
    public static void PreserveFromCurrentPrincipal(
        ClaimsPrincipal? currentPrincipal, ClaimsPrincipal? refreshedPrincipal)
    {
        if (currentPrincipal is null || refreshedPrincipal?.Identity is not ClaimsIdentity identity)
        {
            return;
        }

        PreserveClaim(currentPrincipal, identity, RetaguardaClaims.SiteId);
        PreserveClaim(currentPrincipal, identity, RetaguardaClaims.SiteName);
    }

    private static void PreserveClaim(
        ClaimsPrincipal currentPrincipal, ClaimsIdentity refreshedIdentity, string claimType)
    {
        if (refreshedIdentity.HasClaim(claim => claim.Type == claimType))
        {
            return;
        }

        var claim = currentPrincipal.FindFirst(claimType);
        if (claim is null)
        {
            return;
        }

        refreshedIdentity.AddClaim(
            new Claim(claim.Type, claim.Value, claim.ValueType, claim.Issuer, claim.OriginalIssuer));
    }
}
