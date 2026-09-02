using Microsoft.AspNetCore.Authorization;

namespace Retaguarda.AspNetCore.Authorization;

/// <summary>Exige que o usuário tenha uma permissão específica do catálogo.</summary>
public sealed class PermissionRequirement(string permission) : IAuthorizationRequirement
{
    public string Permission { get; } = permission;
}
