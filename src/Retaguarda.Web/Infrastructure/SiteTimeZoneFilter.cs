using Microsoft.AspNetCore.Mvc.Filters;

namespace Retaguarda.Web.Infrastructure;

/// <summary>
/// Resolve o fuso da planta ativa antes de cada action, para que controllers e views usem
/// <see cref="SiteTimeZone"/> de forma síncrona. É filtro (e não middleware) de propósito: só as
/// requisições de MVC precisam do fuso — arquivo estático e health check, não.
/// </summary>
public sealed class SiteTimeZoneFilter : IAsyncActionFilter
{
    private readonly SiteTimeZone _timeZone;

    public SiteTimeZoneFilter(SiteTimeZone timeZone) => _timeZone = timeZone;

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        await _timeZone.ResolveAsync(context.HttpContext.RequestAborted);
        await next();
    }
}
