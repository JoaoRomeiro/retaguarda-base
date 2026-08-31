using Retaguarda.Business.Roles.Dtos;
using Retaguarda.Shared.Models;

namespace Retaguarda.Business.Roles;

/// <summary>
/// Casos de uso de <see cref="Dtos.RoleDto"/> (CRUD do cadastro de papéis/acessos).
/// Definido em Business e consumido por Web/Api (§4.2).
/// </summary>
public interface IRoleService
{
    Task<PagedResult<RoleListItemDto>> ListAsync(
        string? search, int page, int pageSize, CancellationToken cancellationToken = default);

    Task<RoleDto?> GetByIdAsync(string id, CancellationToken cancellationToken = default);

    // Lança FluentValidation.ValidationException se a entrada for inválida.
    Task<RoleDto> CreateAsync(CreateRoleRequest request, CancellationToken cancellationToken = default);

    // False se o papel não existe; lança ValidationException se a entrada for inválida.
    // Em papéis internos (IsSystem) o nome é preservado — só a descrição é alterável.
    Task<bool> UpdateAsync(UpdateRoleRequest request, CancellationToken cancellationToken = default);

    // Exclusão lógica com guardas: papel interno e papel com usuários vinculados não são excluídos.
    Task<RoleDeletionResult> DeleteAsync(string id, CancellationToken cancellationToken = default);
}
