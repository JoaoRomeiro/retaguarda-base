using FluentValidation;
using Mapster;
using Retaguarda.Business.Roles.Dtos;
using Retaguarda.Data.Identity;
using Retaguarda.Data.Repositories;
using Retaguarda.Shared.Models;

namespace Retaguarda.Business.Roles;

public sealed class RoleService : IRoleService
{
    private const int DefaultPageSize = 20;

    private readonly IRoleRepository _repository;
    private readonly IValidator<CreateRoleRequest> _createValidator;
    private readonly IValidator<UpdateRoleRequest> _updateValidator;

    public RoleService(
        IRoleRepository repository,
        IValidator<CreateRoleRequest> createValidator,
        IValidator<UpdateRoleRequest> updateValidator)
    {
        _repository = repository;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    public async Task<PagedResult<RoleListItemDto>> ListAsync(
        string? search, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        if (page < 1)
        {
            page = 1;
        }

        if (pageSize < 1)
        {
            pageSize = DefaultPageSize;
        }

        var (items, total) = await _repository.ListAsync(search, page, pageSize, cancellationToken);
        var dtos = items.Adapt<List<RoleListItemDto>>();
        return new PagedResult<RoleListItemDto>(dtos, total, page, pageSize);
    }

    public async Task<RoleDto?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        var role = await _repository.GetByIdAsync(id, cancellationToken);
        if (role is null)
        {
            return null;
        }

        var dto = role.Adapt<RoleDto>();
        dto.Permissions = await _repository.GetPermissionsAsync(role.Id, cancellationToken);
        return dto;
    }

    public async Task<RoleDto> CreateAsync(CreateRoleRequest request, CancellationToken cancellationToken = default)
    {
        await _createValidator.ValidateAndThrowAsync(request, cancellationToken);

        // Papéis criados pelo cadastro nunca são internos (IsSystem = false por padrão).
        var role = new ApplicationRole(request.Name) { Description = request.Description };
        var created = await _repository.AddAsync(role, cancellationToken);

        var permissions = Normalize(request.Permissions);
        await _repository.SetPermissionsAsync(created, permissions, cancellationToken);

        var dto = created.Adapt<RoleDto>();
        dto.Permissions = permissions;
        return dto;
    }

    public async Task<bool> UpdateAsync(UpdateRoleRequest request, CancellationToken cancellationToken = default)
    {
        var role = await _repository.GetByIdAsync(request.Id, cancellationToken);
        if (role is null)
        {
            return false;
        }

        await _updateValidator.ValidateAndThrowAsync(request, cancellationToken);

        // Papel interno: nome bloqueado (o código e [Authorize(Roles=...)] dependem dele);
        // só a descrição é alterável. Defesa no servidor além do campo desabilitado na UI.
        if (!role.IsSystem)
        {
            role.Name = request.Name;
        }

        role.Description = request.Description;
        await _repository.UpdateAsync(role, cancellationToken);

        // Papel interno tem as permissões travadas pelo mesmo motivo do nome: o seeder reconcede
        // TODAS a cada boot, e deixar o formulário revogá-las trancaria o administrador para fora
        // até o próximo restart. Defesa no servidor além dos checkboxes desabilitados na UI.
        if (!role.IsSystem)
        {
            await _repository.SetPermissionsAsync(role, Normalize(request.Permissions), cancellationToken);
        }

        return true;
    }

    // Remove duplicatas e entradas vazias vindas do formulário e devolve em ordem estável.
    private static List<string> Normalize(IEnumerable<string>? permissions) =>
        (permissions ?? [])
            .Where(permission => !string.IsNullOrWhiteSpace(permission))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(permission => permission, StringComparer.Ordinal)
            .ToList();

    public async Task<RoleDeletionResult> DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        var role = await _repository.GetByIdAsync(id, cancellationToken);
        if (role is null)
        {
            return RoleDeletionResult.NotFound;
        }

        if (role.IsSystem)
        {
            return RoleDeletionResult.SystemRole;
        }

        if (await _repository.CountUsersInRoleAsync(role.Id, cancellationToken) > 0)
        {
            return RoleDeletionResult.HasAssignedUsers;
        }

        await _repository.DeleteAsync(role, cancellationToken);
        return RoleDeletionResult.Deleted;
    }
}
