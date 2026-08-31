using FluentValidation;
using Retaguarda.Business.Users.Dtos;
using Retaguarda.Data.Identity;
using Retaguarda.Data.Repositories;
using Retaguarda.Shared.Models;

namespace Retaguarda.Business.Users;

public sealed class UserService : IUserService
{
    private const int DefaultPageSize = 20;

    private readonly IUserRepository _repository;
    private readonly IValidator<CreateUserRequest> _createValidator;
    private readonly IValidator<UpdateUserRequest> _updateValidator;

    public UserService(
        IUserRepository repository,
        IValidator<CreateUserRequest> createValidator,
        IValidator<UpdateUserRequest> updateValidator)
    {
        _repository = repository;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    public async Task<PagedResult<UserListItemDto>> ListAsync(
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
        var roleNames = await _repository.GetRoleNamesAsync(
            items.Select(u => u.Id).ToList(), cancellationToken);

        var dtos = items.Select(u => new UserListItemDto
        {
            Id = u.Id,
            FullName = u.FullName,
            Email = u.Email ?? string.Empty,
            RoleName = roleNames.GetValueOrDefault(u.Id),
            DefaultSiteName = u.DefaultSite?.Name,
            IsActive = u.IsActive,
        }).ToList();

        return new PagedResult<UserListItemDto>(dtos, total, page, pageSize);
    }

    public async Task<UserDto?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        var user = await _repository.GetByIdAsync(id, cancellationToken);
        if (user is null)
        {
            return null;
        }

        var roleName = await _repository.GetRoleNameAsync(id, cancellationToken);
        var siteIds = await _repository.GetSiteIdsAsync(id, cancellationToken);

        return new UserDto
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email ?? string.Empty,
            RoleName = roleName,
            DefaultSiteId = user.DefaultSiteId,
            SiteIds = siteIds.ToList(),
            IsActive = user.IsActive,
        };
    }

    public async Task<UserDto> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken = default)
    {
        await _createValidator.ValidateAndThrowAsync(request, cancellationToken);

        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            EmailConfirmed = true,  // criado pelo admin: e-mail considerado válido
            FullName = request.FullName,
            IsActive = request.IsActive,
            DefaultSiteId = request.DefaultSiteId,
        };

        // A planta escolhida é a padrão e também a primeira (única) associação.
        await _repository.AddAsync(user, request.Password, request.RoleName, [request.DefaultSiteId], cancellationToken);

        return new UserDto
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email ?? string.Empty,
            RoleName = request.RoleName,
            DefaultSiteId = user.DefaultSiteId,
            SiteIds = [request.DefaultSiteId],
            IsActive = user.IsActive,
        };
    }

    public async Task<bool> UpdateAsync(UpdateUserRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _repository.GetByIdAsync(request.Id, cancellationToken);
        if (user is null)
        {
            return false;
        }

        await _updateValidator.ValidateAndThrowAsync(request, cancellationToken);

        user.FullName = request.FullName;
        user.IsActive = request.IsActive;
        user.DefaultSiteId = request.DefaultSiteId;

        await _repository.UpdateAsync(user, request.RoleName, cancellationToken);
        return true;
    }

    public async Task<UserDeletionResult> DeleteAsync(
        string id, string? currentUserId, CancellationToken cancellationToken = default)
    {
        // Bloqueia excluir a própria conta (mesmo que exista).
        if (currentUserId is not null && string.Equals(id, currentUserId, StringComparison.Ordinal))
        {
            return UserDeletionResult.SelfDelete;
        }

        var user = await _repository.GetByIdAsync(id, cancellationToken);
        if (user is null)
        {
            return UserDeletionResult.NotFound;
        }

        await _repository.DeleteAsync(user, cancellationToken);
        return UserDeletionResult.Deleted;
    }
}
