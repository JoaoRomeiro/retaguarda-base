using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Retaguarda.Business.Roles;
using Retaguarda.Business.Sites;
using Retaguarda.Business.Users;
using Retaguarda.Business.Users.Dtos;
using Retaguarda.Shared;
using Retaguarda.Shared.Contracts;
using Retaguarda.Web.Models.Users;

namespace Retaguarda.Web.Controllers;

// Cadastro de usuários (User). Restrito a Admin. Um usuário tem uma Role e 1..N plantas,
// com uma planta padrão. O estado da listagem (busca + página) é propagado por todo o fluxo.
[Authorize(Roles = "Admin")]
public sealed class UsersController : Controller
{
    private const int PageSize = 10;
    private const int OptionsPageSize = 1000;  // sites/roles são poucos; carrega todos para os selects

    private readonly IUserService _userService;
    private readonly ISiteService _siteService;
    private readonly IRoleService _roleService;
    private readonly ICurrentUserService _currentUser;
    private readonly IStringLocalizer<SharedResources> _localizer;

    public UsersController(
        IUserService userService,
        ISiteService siteService,
        IRoleService roleService,
        ICurrentUserService currentUser,
        IStringLocalizer<SharedResources> localizer)
    {
        _userService = userService;
        _siteService = siteService;
        _roleService = roleService;
        _currentUser = currentUser;
        _localizer = localizer;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? search, int page = 1, CancellationToken cancellationToken = default)
    {
        var result = await _userService.ListAsync(search, page, PageSize, cancellationToken);
        ViewData["CurrentUserId"] = _currentUser.UserId;
        return View(new UserIndexViewModel { Users = result, Search = search });
    }

    [HttpGet]
    public async Task<IActionResult> Create(string? search, int page = 1, CancellationToken cancellationToken = default)
    {
        SetListState(search, page);
        await PopulateCreateOptionsAsync(cancellationToken);
        return View(new CreateUserRequest { IsActive = true });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        CreateUserRequest request, string? search, int page = 1, CancellationToken cancellationToken = default)
    {
        try
        {
            await _userService.CreateAsync(request, cancellationToken);
        }
        catch (ValidationException ex)
        {
            AddValidationErrors(ex);
            SetListState(search, page);
            await PopulateCreateOptionsAsync(cancellationToken);
            return View(request);
        }

        TempData["StatusMessage"] = _localizer["user_created"].Value;
        return RedirectToAction(nameof(Index), new { search, page });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(string id, string? search, int page = 1, CancellationToken cancellationToken = default)
    {
        var user = await _userService.GetByIdAsync(id, cancellationToken);
        if (user is null)
        {
            return NotFound();
        }

        SetListState(search, page);
        await PopulateEditOptionsAsync(user.SiteIds, cancellationToken);
        ViewData["Email"] = user.Email;  // e-mail é somente-leitura na edição
        return View(new UpdateUserRequest
        {
            Id = user.Id,
            FullName = user.FullName,
            RoleName = user.RoleName ?? string.Empty,
            DefaultSiteId = user.DefaultSiteId,
            IsActive = user.IsActive,
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        UpdateUserRequest request, string? search, int page = 1, CancellationToken cancellationToken = default)
    {
        UserUpdateResult result;
        try
        {
            result = await _userService.UpdateAsync(request, _currentUser.UserId, cancellationToken);
        }
        catch (ValidationException ex)
        {
            AddValidationErrors(ex);
            return await RedisplayEditAsync(request, search, page, cancellationToken);
        }

        switch (result)
        {
            case UserUpdateResult.NotFound:
                return NotFound();

            // Recusas de regra de negócio: a entrada é válida, a operação é que não pode.
            // Volta para o formulário com a edição preservada, no mesmo caminho da validação.
            case UserUpdateResult.SelfDeactivate:
                ModelState.AddModelError(string.Empty, _localizer["user_self_deactivate"].Value);
                return await RedisplayEditAsync(request, search, page, cancellationToken);

            case UserUpdateResult.SelfRoleChange:
                ModelState.AddModelError(string.Empty, _localizer["user_self_role_change"].Value);
                return await RedisplayEditAsync(request, search, page, cancellationToken);

            case UserUpdateResult.LastAdmin:
                ModelState.AddModelError(string.Empty, _localizer["user_last_admin"].Value);
                return await RedisplayEditAsync(request, search, page, cancellationToken);

            default:
                TempData["StatusMessage"] = _localizer["user_updated"].Value;
                return RedirectToAction(nameof(Index), new { search, page });
        }
    }

    // Reexibe o formulário de edição preservando o que o usuário digitou, o estado da listagem
    // e as opções dos selects. Usado por todos os caminhos de recusa da edição.
    private async Task<IActionResult> RedisplayEditAsync(
        UpdateUserRequest request, string? search, int page, CancellationToken cancellationToken)
    {
        SetListState(search, page);
        var current = await _userService.GetByIdAsync(request.Id, cancellationToken);
        await PopulateEditOptionsAsync(current?.SiteIds ?? [], cancellationToken);
        ViewData["Email"] = current?.Email;
        return View(request);
    }

    [HttpGet]
    public async Task<IActionResult> Delete(string id, string? search, int page = 1, CancellationToken cancellationToken = default)
    {
        var user = await _userService.GetByIdAsync(id, cancellationToken);
        if (user is null)
        {
            return NotFound();
        }

        // Não exibe a confirmação para a própria conta.
        if (string.Equals(id, _currentUser.UserId, StringComparison.Ordinal))
        {
            TempData["ErrorMessage"] = _localizer["user_delete_self"].Value;
            return RedirectToAction(nameof(Index), new { search, page });
        }

        SetListState(search, page);
        return View(user);
    }

    [HttpPost]
    [ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(
        string id, string? search, int page = 1, CancellationToken cancellationToken = default)
    {
        var result = await _userService.DeleteAsync(id, _currentUser.UserId, cancellationToken);

        switch (result)
        {
            case UserDeletionResult.Deleted:
                TempData["StatusMessage"] = _localizer["user_deleted"].Value;
                break;

            case UserDeletionResult.NotFound:
                return NotFound();

            case UserDeletionResult.SelfDelete:
                TempData["ErrorMessage"] = _localizer["user_delete_self"].Value;
                break;
        }

        return RedirectToAction(nameof(Index), new { search, page });
    }

    // Acessos para o select de role.
    private async Task PopulateRolesAsync(CancellationToken cancellationToken)
    {
        var roles = await _roleService.ListAsync(null, 1, OptionsPageSize, cancellationToken);
        ViewData["Roles"] = roles.Items.Select(r => r.Name).ToList();
    }

    private async Task<List<SiteOption>> AllSiteOptionsAsync(CancellationToken cancellationToken)
    {
        var sites = await _siteService.ListAsync(null, 1, OptionsPageSize, cancellationToken);
        return sites.Items.Select(s => new SiteOption(s.Id, s.Name)).ToList();
    }

    // Create: a planta escolhida vira a padrão — oferece todas as plantas.
    private async Task PopulateCreateOptionsAsync(CancellationToken cancellationToken)
    {
        ViewData["Sites"] = await AllSiteOptionsAsync(cancellationToken);
        await PopulateRolesAsync(cancellationToken);
    }

    // Edit: a planta padrão só pode ser uma das já associadas ao usuário.
    private async Task PopulateEditOptionsAsync(
        IReadOnlyCollection<int> linkedSiteIds, CancellationToken cancellationToken)
    {
        var all = await AllSiteOptionsAsync(cancellationToken);
        ViewData["Sites"] = all.Where(o => linkedSiteIds.Contains(o.Id)).ToList();
        await PopulateRolesAsync(cancellationToken);
    }

    // Disponibiliza o estado da listagem para a view (campos ocultos + link Cancelar).
    private void SetListState(string? search, int page)
    {
        ViewData["ListSearch"] = search;
        ViewData["ListPage"] = page;
    }

    // Traduz as falhas do validator (cujas mensagens são chaves de recurso) para o ModelState.
    private void AddValidationErrors(ValidationException exception)
    {
        foreach (var error in exception.Errors)
        {
            ModelState.AddModelError(error.PropertyName, _localizer[error.ErrorMessage].Value);
        }
    }
}
