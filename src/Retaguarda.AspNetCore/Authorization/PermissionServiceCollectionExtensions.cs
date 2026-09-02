using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Retaguarda.Shared.Authorization;

namespace Retaguarda.AspNetCore.Authorization;

public static class PermissionServiceCollectionExtensions
{
    /// <summary>
    /// Registra o catálogo de permissões e a autorização por permissão. Já inclui as permissões da
    /// plataforma; o projeto derivado acrescenta as dele registrando outro
    /// <see cref="IPermissionProvider"/> no DI (a ordem não importa — o catálogo reúne todos).
    /// </summary>
    public static IServiceCollection AddRetaguardaPermissions(this IServiceCollection services)
    {
        services.AddSingleton<IPermissionProvider, PlatformPermissions.Provider>();
        services.AddSingleton<IPermissionCatalog, PermissionCatalog>();

        // Singleton: a decisão só lê claims do principal, não tem estado por requisição.
        services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();

        // TryAdd... não serve aqui: AddAuthorization() já registrou o provider padrão e a intenção
        // é justamente substituí-lo.
        services.Replace(ServiceDescriptor.Singleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>());

        return services;
    }
}
