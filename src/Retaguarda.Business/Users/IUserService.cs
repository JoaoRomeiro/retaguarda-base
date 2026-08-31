using Retaguarda.Business.Users.Dtos;
using Retaguarda.Shared.Models;

namespace Retaguarda.Business.Users;

/// <summary>
/// Casos de uso de <see cref="Dtos.UserDto"/> (CRUD do cadastro de usuários).
/// Definido em Business e consumido por Web/Api (§4.2).
/// </summary>
public interface IUserService
{
    Task<PagedResult<UserListItemDto>> ListAsync(
        string? search, int page, int pageSize, CancellationToken cancellationToken = default);

    Task<UserDto?> GetByIdAsync(string id, CancellationToken cancellationToken = default);

    // Lança FluentValidation.ValidationException se a entrada for inválida.
    Task<UserDto> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken = default);

    // False se o usuário não existe; lança ValidationException se a entrada for inválida.
    Task<bool> UpdateAsync(UpdateUserRequest request, CancellationToken cancellationToken = default);

    // Exclusão lógica. Bloqueia excluir a própria conta (currentUserId).
    Task<UserDeletionResult> DeleteAsync(
        string id, string? currentUserId, CancellationToken cancellationToken = default);
}
