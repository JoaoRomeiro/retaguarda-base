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

    // Atualiza o perfil e o papel. Lança ValidationException se a entrada for inválida; recusa
    // (sem exceção) as operações que deixariam o sistema inconsistente — ver UserUpdateResult.
    // currentUserId identifica quem está editando, para barrar a autoedição destrutiva.
    Task<UserUpdateResult> UpdateAsync(
        UpdateUserRequest request, string? currentUserId, CancellationToken cancellationToken = default);

    // Exclusão lógica. Bloqueia excluir a própria conta (currentUserId).
    Task<UserDeletionResult> DeleteAsync(
        string id, string? currentUserId, CancellationToken cancellationToken = default);
}
