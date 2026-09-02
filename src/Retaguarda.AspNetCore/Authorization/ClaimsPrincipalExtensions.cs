using System.Security.Claims;
using Retaguarda.Shared;

namespace Retaguarda.AspNetCore.Authorization;

public static class ClaimsPrincipalExtensions
{
    /// <summary>
    /// Diz se o usuário tem a permissão. É o que as views devem usar para esconder menu, botão e
    /// coluna de ação — deixar visível o que a pessoa não pode usar faz o sistema parecer quebrado.
    /// </summary>
    public static bool HasPermission(this ClaimsPrincipal? user, string permission)
    {
        if (user?.Identity?.IsAuthenticated != true || string.IsNullOrWhiteSpace(permission))
        {
            return false;
        }

        return user.HasClaim(RetaguardaClaims.Permission, permission);
    }
}
