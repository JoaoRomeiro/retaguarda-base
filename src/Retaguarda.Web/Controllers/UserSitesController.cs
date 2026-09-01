using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Retaguarda.Business.Sites;
using Retaguarda.Business.Users;
using Retaguarda.Business.Users.Dtos;
using Retaguarda.Shared;
using Retaguarda.Web.Models.Users;

namespace Retaguarda.Web.Controllers;

// Sub-CRUD das plantas associadas a um usuário (R/C/D). Restrito a Admin.
// Carrega dois contextos de estado: o do index de Usuários (userSearch/userPage), usado pelo
// botão Voltar, e o próprio (search/page) da listagem de associação.
[Authorize(Roles = RetaguardaRoles.Admin)]
public sealed class UserSitesController : Controller
{
    private const int PageSize = 10;

    private readonly IUserService _userService;
    private readonly IUserSiteService _userSiteService;
    private readonly ISiteService _siteService;
    private readonly IStringLocalizer<SharedResources> _localizer;

    public UserSitesController(
        IUserService userService,
        IUserSiteService userSiteService,
        ISiteService siteService,
        IStringLocalizer<SharedResources> localizer)
    {
        _userService = userService;
        _userSiteService = userSiteService;
        _siteService = siteService;
        _localizer = localizer;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        string userId, string? userSearch, int userPage = 1, string? search = null, int page = 1,
        CancellationToken cancellationToken = default)
    {
        var user = await _userService.GetByIdAsync(userId, cancellationToken);
        if (user is null)
        {
            return NotFound();
        }

        var sites = await _userSiteService.ListAsync(userId, search, page, PageSize, cancellationToken);
        SetState(userId, userSearch, userPage, search, page);

        return View(new UserSiteIndexViewModel
        {
            UserId = userId,
            UserName = user.FullName,
            Sites = sites,
            Search = search,
        });
    }

    [HttpGet]
    public async Task<IActionResult> Create(
        string userId, string? userSearch, int userPage = 1, string? search = null, int page = 1,
        CancellationToken cancellationToken = default)
    {
        var user = await _userService.GetByIdAsync(userId, cancellationToken);
        if (user is null)
        {
            return NotFound();
        }

        SetState(userId, userSearch, userPage, search, page);
        ViewData["UserName"] = user.FullName;
        ViewData["AvailableSites"] = await _userSiteService.GetAvailableSitesAsync(userId, cancellationToken);
        return View(new AssociateSiteRequest { UserId = userId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        AssociateSiteRequest request, string? userSearch, int userPage = 1, string? search = null, int page = 1,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _userSiteService.AddAsync(request, cancellationToken);
        }
        catch (ValidationException ex)
        {
            AddValidationErrors(ex);
            SetState(request.UserId, userSearch, userPage, search, page);
            var user = await _userService.GetByIdAsync(request.UserId, cancellationToken);
            ViewData["UserName"] = user?.FullName;
            ViewData["AvailableSites"] = await _userSiteService.GetAvailableSitesAsync(request.UserId, cancellationToken);
            return View(request);
        }

        TempData["StatusMessage"] = _localizer["usersite_created"].Value;
        return RedirectToAction(nameof(Index), new { userId = request.UserId, userSearch, userPage, search, page });
    }

    [HttpGet]
    public async Task<IActionResult> Delete(
        string userId, int siteId, string? userSearch, int userPage = 1, string? search = null, int page = 1,
        CancellationToken cancellationToken = default)
    {
        var user = await _userService.GetByIdAsync(userId, cancellationToken);
        if (user is null)
        {
            return NotFound();
        }

        // A planta padrão não pode ser removida: nem exibe a confirmação.
        if (siteId == user.DefaultSiteId)
        {
            TempData["ErrorMessage"] = _localizer["usersite_delete_default"].Value;
            return RedirectToAction(nameof(Index), new { userId, userSearch, userPage, search, page });
        }

        var site = await _siteService.GetByIdAsync(siteId, cancellationToken);
        if (site is null)
        {
            return NotFound();
        }

        SetState(userId, userSearch, userPage, search, page);
        ViewData["UserName"] = user.FullName;
        return View(new UserSiteDeleteViewModel
        {
            UserId = userId,
            SiteId = siteId,
            SiteName = site.Name,
            SiteCode = site.Code,
        });
    }

    [HttpPost]
    [ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(
        string userId, int siteId, string? userSearch, int userPage = 1, string? search = null, int page = 1,
        CancellationToken cancellationToken = default)
    {
        var result = await _userSiteService.RemoveAsync(userId, siteId, cancellationToken);

        switch (result)
        {
            case SiteUnlinkResult.Removed:
                TempData["StatusMessage"] = _localizer["usersite_deleted"].Value;
                break;

            case SiteUnlinkResult.IsDefault:
                TempData["ErrorMessage"] = _localizer["usersite_delete_default"].Value;
                break;

            case SiteUnlinkResult.NotFound:
                // Vínculo já não existe — apenas volta à listagem.
                break;
        }

        return RedirectToAction(nameof(Index), new { userId, userSearch, userPage, search, page });
    }

    // Estado: contexto do index de Usuários (userSearch/userPage, para o Voltar) + o próprio (search/page).
    private void SetState(string userId, string? userSearch, int userPage, string? search, int page)
    {
        ViewData["UserId"] = userId;
        ViewData["UserSearch"] = userSearch;
        ViewData["UserPage"] = userPage;
        ViewData["ListSearch"] = search;
        ViewData["ListPage"] = page;
    }

    private void AddValidationErrors(ValidationException exception)
    {
        foreach (var error in exception.Errors)
        {
            ModelState.AddModelError(error.PropertyName, _localizer[error.ErrorMessage].Value);
        }
    }
}
