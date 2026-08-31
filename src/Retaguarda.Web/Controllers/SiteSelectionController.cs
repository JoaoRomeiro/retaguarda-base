using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Retaguarda.Business.Users;
using Retaguarda.Shared;
using Retaguarda.Web.Models.SiteSelection;

namespace Retaguarda.Web.Controllers;

// Seleção obrigatória da planta ativa após o login (roadmap 2.2.1). Exige autenticação,
// mas é liberada pelo gate (ActiveSiteSelectionMiddleware) por ainda não ter a planta ativa.
[Authorize]
public sealed class SiteSelectionController : Controller
{
    private readonly ISiteSelectionService _siteSelectionService;
    private readonly IUserService _userService;
    private readonly IStringLocalizer<SharedResources> _localizer;

    public SiteSelectionController(
        ISiteSelectionService siteSelectionService,
        IUserService userService,
        IStringLocalizer<SharedResources> localizer)
    {
        _siteSelectionService = siteSelectionService;
        _userService = userService;
        _localizer = localizer;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? returnUrl = null, CancellationToken cancellationToken = default)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
        {
            return Forbid();
        }

        var sites = await _siteSelectionService.GetSelectableSitesAsync(userId, cancellationToken);

        // Pré-seleciona a planta padrão do usuário, se ela estiver entre as selecionáveis.
        var user = await _userService.GetByIdAsync(userId, cancellationToken);
        var defaultSiteId = user?.DefaultSiteId ?? 0;
        var preselected = sites.Any(s => s.Id == defaultSiteId) ? defaultSiteId : 0;

        return View(new SiteSelectionViewModel
        {
            ReturnUrl = returnUrl,
            Sites = sites,
            SiteId = preselected,
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Select(
        SiteSelectionViewModel model, CancellationToken cancellationToken = default)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
        {
            return Forbid();
        }

        var sites = await _siteSelectionService.GetSelectableSitesAsync(userId, cancellationToken);
        if (model.SiteId <= 0)
        {
            ModelState.AddModelError(nameof(model.SiteId), _localizer["site_selection_required"].Value);
            model.Sites = sites;
            return View(nameof(Index), model);
        }

        var selectedSite = sites.FirstOrDefault(s => s.Id == model.SiteId);
        if (selectedSite is null)
        {
            ModelState.AddModelError(nameof(model.SiteId), _localizer["site_selection_invalid"].Value);
            model.Sites = sites;
            return View(nameof(Index), model);
        }

        // Grava a planta ativa como claim, re-emitindo o cookie e preservando as propriedades
        // de autenticação atuais (persistência/expiração).
        var authResult = await HttpContext.AuthenticateAsync(IdentityConstants.ApplicationScheme);
        var identity = (ClaimsIdentity)User.Identity!;
        var existingSiteId = identity.FindFirst(RetaguardaClaims.SiteId);
        if (existingSiteId is not null)
        {
            identity.RemoveClaim(existingSiteId);
        }

        var existingSiteName = identity.FindFirst(RetaguardaClaims.SiteName);
        if (existingSiteName is not null)
        {
            identity.RemoveClaim(existingSiteName);
        }

        identity.AddClaim(new Claim(RetaguardaClaims.SiteId, model.SiteId.ToString(CultureInfo.InvariantCulture)));
        identity.AddClaim(new Claim(RetaguardaClaims.SiteName, selectedSite.Name));
        await HttpContext.SignInAsync(
            IdentityConstants.ApplicationScheme,
            new ClaimsPrincipal(identity),
            authResult.Properties ?? new AuthenticationProperties());

        return RedirectToLocal(model.ReturnUrl);
    }

    // Evita open redirect: só aceita URLs locais.
    private IActionResult RedirectToLocal(string? returnUrl)
    {
        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction("Index", "Home");
    }
}
