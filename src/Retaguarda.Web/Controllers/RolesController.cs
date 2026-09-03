using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Retaguarda.Business.Roles;
using Retaguarda.Business.Roles.Dtos;
using Retaguarda.Shared;
using Retaguarda.Shared.Authorization;
using Retaguarda.Web.Models.Roles;

namespace Retaguarda.Web.Controllers;

// Cadastro de acessos (Role). Cada ação exige a permissão do seu próprio verbo — quem edita
// acessos decide quem pode o quê, então é o cadastro mais sensível da base.
// O estado da listagem (busca + página) é propagado por todo o fluxo para que o usuário
// volte sempre ao mesmo ponto após criar/editar/excluir/cancelar.
[Authorize]
public sealed class RolesController : Controller
{
    private const int PageSize = 10;

    private readonly IRoleService _roleService;
    private readonly IPermissionCatalog _permissions;
    private readonly IStringLocalizer<SharedResources> _localizer;

    public RolesController(
        IRoleService roleService,
        IPermissionCatalog permissions,
        IStringLocalizer<SharedResources> localizer)
    {
        _roleService = roleService;
        _permissions = permissions;
        _localizer = localizer;
    }

    [HttpGet]
    [Authorize(Policy = PlatformPermissions.Roles.View)]
    public async Task<IActionResult> Index(string? search, int page = 1, CancellationToken cancellationToken = default)
    {
        var result = await _roleService.ListAsync(search, page, PageSize, cancellationToken);
        return View(new RoleIndexViewModel { Roles = result, Search = search });
    }

    [HttpGet]
    [Authorize(Policy = PlatformPermissions.Roles.Create)]
    public IActionResult Create(string? search, int page = 1)
    {
        SetListState(search, page);
        SetPermissionState(isSystem: false);
        return View(new CreateRoleRequest());
    }

    [HttpPost]
    [Authorize(Policy = PlatformPermissions.Roles.Create)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        CreateRoleRequest request, string? search, int page = 1, CancellationToken cancellationToken = default)
    {
        try
        {
            await _roleService.CreateAsync(request, cancellationToken);
        }
        catch (ValidationException ex)
        {
            AddValidationErrors(ex);
            SetListState(search, page);
            SetPermissionState(isSystem: false);
            return View(request);
        }

        TempData["StatusMessage"] = _localizer["role_created"].Value;
        return RedirectToAction(nameof(Index), new { search, page });
    }

    [HttpGet]
    [Authorize(Policy = PlatformPermissions.Roles.Edit)]
    public async Task<IActionResult> Edit(string id, string? search, int page = 1, CancellationToken cancellationToken = default)
    {
        var role = await _roleService.GetByIdAsync(id, cancellationToken);
        if (role is null)
        {
            return NotFound();
        }

        SetListState(search, page);
        // IsSystem controla o bloqueio do nome e das permissões na view (papel interno é
        // somente-leitura nesses dois campos).
        SetPermissionState(role.IsSystem);
        return View(new UpdateRoleRequest
        {
            Id = role.Id,
            Name = role.Name,
            Description = role.Description,
            Permissions = [.. role.Permissions],
        });
    }

    [HttpPost]
    [Authorize(Policy = PlatformPermissions.Roles.Edit)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        UpdateRoleRequest request, string? search, int page = 1, CancellationToken cancellationToken = default)
    {
        bool updated;
        try
        {
            updated = await _roleService.UpdateAsync(request, cancellationToken);
        }
        catch (ValidationException ex)
        {
            AddValidationErrors(ex);
            SetListState(search, page);
            // Reavalia IsSystem para re-renderizar o formulário corretamente após erro.
            var current = await _roleService.GetByIdAsync(request.Id, cancellationToken);
            SetPermissionState(current?.IsSystem ?? false);
            return View(request);
        }

        if (!updated)
        {
            return NotFound();
        }

        TempData["StatusMessage"] = _localizer["role_updated"].Value;
        return RedirectToAction(nameof(Index), new { search, page });
    }

    [HttpGet]
    [Authorize(Policy = PlatformPermissions.Roles.Delete)]
    public async Task<IActionResult> Delete(string id, string? search, int page = 1, CancellationToken cancellationToken = default)
    {
        var role = await _roleService.GetByIdAsync(id, cancellationToken);
        if (role is null)
        {
            return NotFound();
        }

        // Papel interno não é excluível: nem exibe a tela de confirmação.
        if (role.IsSystem)
        {
            TempData["ErrorMessage"] = _localizer["role_delete_system"].Value;
            return RedirectToAction(nameof(Index), new { search, page });
        }

        SetListState(search, page);
        return View(role);
    }

    [HttpPost]
    [Authorize(Policy = PlatformPermissions.Roles.Delete)]
    [ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(
        string id, string? search, int page = 1, CancellationToken cancellationToken = default)
    {
        var result = await _roleService.DeleteAsync(id, cancellationToken);

        switch (result)
        {
            case RoleDeletionResult.Deleted:
                TempData["StatusMessage"] = _localizer["role_deleted"].Value;
                break;

            case RoleDeletionResult.NotFound:
                return NotFound();

            case RoleDeletionResult.SystemRole:
                TempData["ErrorMessage"] = _localizer["role_delete_system"].Value;
                break;

            case RoleDeletionResult.HasAssignedUsers:
                TempData["ErrorMessage"] = _localizer["role_delete_has_users"].Value;
                break;
        }

        return RedirectToAction(nameof(Index), new { search, page });
    }

    // Catálogo de permissões (checkboxes) + bloqueio do papel interno.
    private void SetPermissionState(bool isSystem)
    {
        ViewData["IsSystem"] = isSystem;
        ViewData["PermissionCatalog"] = _permissions;
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
