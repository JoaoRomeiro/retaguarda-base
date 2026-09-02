using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Retaguarda.Shared.Authorization;

namespace Retaguarda.AspNetCore.Authorization;

/// <summary>
/// Cria a política de autorização sob demanda a partir do nome da permissão, para que
/// <c>[Authorize(Policy = PlatformPermissions.Sites.Edit)]</c> funcione sem registrar uma política
/// por permissão no <c>Program.cs</c> (seriam dezenas, e cada permissão nova exigiria lembrar de
/// registrar a dela).
///
/// Nome que NÃO está no catálogo cai no provider padrão — assim políticas nomeadas comuns
/// continuam funcionando, e um nome inexistente não vira permissão fantasma.
/// </summary>
public sealed class PermissionPolicyProvider : IAuthorizationPolicyProvider
{
    private readonly DefaultAuthorizationPolicyProvider _fallback;
    private readonly IPermissionCatalog _catalog;

    public PermissionPolicyProvider(IOptions<AuthorizationOptions> options, IPermissionCatalog catalog)
    {
        _fallback = new DefaultAuthorizationPolicyProvider(options);
        _catalog = catalog;
    }

    public Task<AuthorizationPolicy> GetDefaultPolicyAsync() => _fallback.GetDefaultPolicyAsync();

    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync() => _fallback.GetFallbackPolicyAsync();

    public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (_catalog.Contains(policyName))
        {
            var policy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .AddRequirements(new PermissionRequirement(policyName))
                .Build();

            return Task.FromResult<AuthorizationPolicy?>(policy);
        }

        return _fallback.GetPolicyAsync(policyName);
    }
}
