using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Retaguarda.AspNetCore.Authorization;
using Retaguarda.Shared;
using Retaguarda.Shared.Authorization;

namespace Retaguarda.UnitTests.Authorization;

public sealed class PermissionAuthorizationTests
{
    private static ClaimsPrincipal Authenticated(params string[] permissions)
    {
        var claims = permissions.Select(p => new Claim(RetaguardaClaims.Permission, p));

        // O authenticationType não-nulo é o que torna a identidade autenticada.
        return new ClaimsPrincipal(new ClaimsIdentity(claims, "TestCookie"));
    }

    private static ClaimsPrincipal Anonymous() => new(new ClaimsIdentity());

    private static async Task<bool> EvaluateAsync(ClaimsPrincipal user, string permission)
    {
        var context = new AuthorizationHandlerContext(
            [new PermissionRequirement(permission)],
            user,
            resource: null);

        await new PermissionAuthorizationHandler().HandleAsync(context);
        return context.HasSucceeded;
    }

    [Fact]
    public async Task Grants_access_when_the_user_has_the_permission()
    {
        var user = Authenticated(PlatformPermissions.Sites.Edit);

        Assert.True(await EvaluateAsync(user, PlatformPermissions.Sites.Edit));
    }

    [Fact]
    public async Task Denies_access_when_the_user_has_another_permission()
    {
        var user = Authenticated(PlatformPermissions.Sites.View);

        Assert.False(await EvaluateAsync(user, PlatformPermissions.Sites.Edit));
    }

    [Fact]
    public async Task Denies_access_to_anonymous_user()
    {
        Assert.False(await EvaluateAsync(Anonymous(), PlatformPermissions.Sites.View));
    }

    [Fact]
    public void HasPermission_is_case_sensitive_and_exact()
    {
        var user = Authenticated(PlatformPermissions.Sites.View);

        Assert.True(user.HasPermission("sites.view"));
        Assert.False(user.HasPermission("Sites.View"));
        Assert.False(user.HasPermission("sites"));
        Assert.False(user.HasPermission(""));
    }

    [Fact]
    public void HasPermission_denies_a_null_user()
    {
        ClaimsPrincipal? user = null;

        Assert.False(user.HasPermission(PlatformPermissions.Sites.View));
    }

    [Fact]
    public async Task Policy_provider_builds_a_policy_for_a_catalog_permission()
    {
        var provider = BuildPolicyProvider();

        var policy = await provider.GetPolicyAsync(PlatformPermissions.Sites.Edit);

        Assert.NotNull(policy);
        var requirement = Assert.Single(policy.Requirements.OfType<PermissionRequirement>());
        Assert.Equal(PlatformPermissions.Sites.Edit, requirement.Permission);
    }

    [Fact]
    public async Task Policy_provider_ignores_a_name_outside_the_catalog()
    {
        // Um nome digitado errado não pode virar política: viraria uma permissão fantasma que
        // ninguém tem e ninguém consegue conceder.
        var provider = BuildPolicyProvider();

        Assert.Null(await provider.GetPolicyAsync("sites.edt"));
    }

    private static PermissionPolicyProvider BuildPolicyProvider() =>
        new(Options.Create(new AuthorizationOptions()),
            new PermissionCatalog([new PlatformPermissions.Provider()]));
}
