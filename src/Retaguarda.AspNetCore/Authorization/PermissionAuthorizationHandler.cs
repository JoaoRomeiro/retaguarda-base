using Microsoft.AspNetCore.Authorization;
using Retaguarda.Shared;

namespace Retaguarda.AspNetCore.Authorization;

/// <summary>
/// Decide se o usuário tem a permissão exigida. As permissões chegam como claims do tipo
/// <see cref="RetaguardaClaims.Permission"/>, herdadas do papel: o
/// <c>UserClaimsPrincipalFactory&lt;TUser,TRole&gt;</c> do Identity copia as claims de cada papel do
/// usuário para o principal, então basta ler o principal — sem ida ao banco a cada requisição.
/// </summary>
public sealed class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        if (context.User.HasPermission(requirement.Permission))
        {
            context.Succeed(requirement);
        }

        // Não chamamos Fail(): outro handler pode conceder a mesma exigência por outro caminho.
        // Sem Succeed, o pedido é negado do mesmo jeito no fim da avaliação.
        return Task.CompletedTask;
    }
}
