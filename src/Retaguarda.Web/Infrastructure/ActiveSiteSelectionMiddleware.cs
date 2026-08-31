using Microsoft.AspNetCore.Mvc.Controllers;
using Retaguarda.Shared;

namespace Retaguarda.Web.Infrastructure;

// Gate da planta ativa (roadmap 2.2.1): usuário autenticado que ainda não selecionou a planta
// é redirecionado para a tela de seleção em QUALQUER rota MVC (inclusive acesso direto por URL),
// exceto as telas que não dependem da planta (seleção em si e Account: login/logout/etc.).
public sealed class ActiveSiteSelectionMiddleware
{
    private readonly RequestDelegate _next;

    public ActiveSiteSelectionMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        // Só atua em endpoints MVC; estáticos, /health etc. passam direto.
        var action = context.GetEndpoint()?.Metadata.GetMetadata<ControllerActionDescriptor>();
        if (action is null
            || context.User.Identity?.IsAuthenticated != true
            || action.ControllerName is "SiteSelection" or "Account"
            || context.User.HasClaim(c => c.Type == RetaguardaClaims.SiteId))
        {
            await _next(context);
            return;
        }

        var returnUrl = context.Request.Path + context.Request.QueryString;
        context.Response.Redirect($"/SiteSelection?returnUrl={Uri.EscapeDataString(returnUrl)}");
    }
}
