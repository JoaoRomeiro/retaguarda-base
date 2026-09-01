using FluentValidation;
using Retaguarda.Business.Users.Dtos;
using Retaguarda.Data.Identity;
using Retaguarda.Data.Repositories;
using Retaguarda.Shared;
using Retaguarda.Shared.Models;

namespace Retaguarda.Business.Users;

public sealed class UserService : IUserService
{
    private const int DefaultPageSize = 20;

    private readonly IUserRepository _repository;
    private readonly IRefreshTokenRepository _refreshTokens;
    private readonly IValidator<CreateUserRequest> _createValidator;
    private readonly IValidator<UpdateUserRequest> _updateValidator;

    public UserService(
        IUserRepository repository,
        IRefreshTokenRepository refreshTokens,
        IValidator<CreateUserRequest> createValidator,
        IValidator<UpdateUserRequest> updateValidator)
    {
        _repository = repository;
        _refreshTokens = refreshTokens;
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

    public async Task<UserUpdateResult> UpdateAsync(
        UpdateUserRequest request, string? currentUserId, CancellationToken cancellationToken = default)
    {
        var user = await _repository.GetByIdAsync(request.Id, cancellationToken);
        if (user is null)
        {
            return UserUpdateResult.NotFound;
        }

        await _updateValidator.ValidateAndThrowAsync(request, cancellationToken);

        var currentRole = await _repository.GetRoleNameAsync(user.Id, cancellationToken);
        var isSelf = currentUserId is not null
            && string.Equals(user.Id, currentUserId, StringComparison.Ordinal);
        var losesRole = !string.Equals(currentRole, request.RoleName, StringComparison.Ordinal);
        var isBeingDeactivated = user.IsActive && !request.IsActive;

        // Autoedição destrutiva: quem está logado se trancaria para fora, sem caminho de volta
        // pela interface. Barrado no SERVIÇO — esconder o campo na view é conveniência, não garantia.
        if (isSelf && isBeingDeactivated)
        {
            return UserUpdateResult.SelfDeactivate;
        }

        if (isSelf && losesRole)
        {
            return UserUpdateResult.SelfRoleChange;
        }

        // Último administrador: desativá-lo ou tirar o papel dele deixaria o sistema sem ninguém
        // capaz de gerenciar usuários — só daria para recuperar por SQL no banco.
        var loosensAdmin = string.Equals(currentRole, RetaguardaRoles.Admin, StringComparison.Ordinal)
            && user.IsActive
            && (isBeingDeactivated || losesRole);
        if (loosensAdmin && !await _repository.HasOtherActiveAdminAsync(user.Id, cancellationToken))
        {
            return UserUpdateResult.LastAdmin;
        }

        // Estado anterior: só a TRANSIÇÃO ativo → inativo derruba as sessões (reeditar um
        // usuário que já estava inativo não precisa revogar nada de novo).
        var wasActive = user.IsActive;

        user.FullName = request.FullName;
        user.IsActive = request.IsActive;
        user.DefaultSiteId = request.DefaultSiteId;

        await _repository.UpdateAsync(user, request.RoleName, cancellationToken);

        if (wasActive && !user.IsActive)
        {
            await RevokeAccessAsync(user, cancellationToken);
        }

        return UserUpdateResult.Updated;
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

        // Simétrico à guarda do UpdateAsync: excluir o último admin ativo deixaria o sistema sem
        // ninguém capaz de gerenciar usuários. Admin já inativo não conta — o sistema já está sem
        // ele, e a recuperação nesse caso passa pelo banco de qualquer forma.
        var role = await _repository.GetRoleNameAsync(user.Id, cancellationToken);
        if (string.Equals(role, RetaguardaRoles.Admin, StringComparison.Ordinal)
            && user.IsActive
            && !await _repository.HasOtherActiveAdminAsync(user.Id, cancellationToken))
        {
            return UserDeletionResult.LastAdmin;
        }

        await _repository.DeleteAsync(user, cancellationToken);
        return UserDeletionResult.Deleted;
    }

    // Encerra as sessões abertas de um usuário que acabou de ser desativado. Bloquear o próximo
    // login (ApplicationSignInManager) não basta: o cookie e o refresh token já emitidos
    // continuariam valendo por horas.
    //   - Web: regenerar o security stamp faz o cookie deixar de bater e o
    //     SecurityStampValidator rejeitar o principal na próxima validação.
    //   - Api: revogar os refresh tokens impede a renovação (o access token restante expira
    //     sozinho em minutos).
    // A EXCLUSÃO do usuário não precisa disso: o soft delete some com ele do query filter, o
    // stamp não é encontrado e a rejeição do cookie acontece pelo mesmo caminho.
    private async Task RevokeAccessAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        await _repository.RegenerateSecurityStampAsync(user, cancellationToken);
        await _refreshTokens.RevokeAllForUserAsync(user.Id, cancellationToken);
    }
}
