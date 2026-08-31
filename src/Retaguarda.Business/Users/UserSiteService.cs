using FluentValidation;
using Retaguarda.Business.Users.Dtos;
using Retaguarda.Data.Repositories;
using Retaguarda.Shared.Models;

namespace Retaguarda.Business.Users;

public sealed class UserSiteService : IUserSiteService
{
    private const int DefaultPageSize = 20;

    private readonly IUserRepository _repository;
    private readonly IValidator<AssociateSiteRequest> _associateValidator;

    public UserSiteService(IUserRepository repository, IValidator<AssociateSiteRequest> associateValidator)
    {
        _repository = repository;
        _associateValidator = associateValidator;
    }

    public async Task<PagedResult<UserSiteListItemDto>> ListAsync(
        string userId, string? search, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        if (page < 1)
        {
            page = 1;
        }

        if (pageSize < 1)
        {
            pageSize = DefaultPageSize;
        }

        var user = await _repository.GetByIdAsync(userId, cancellationToken);
        var defaultSiteId = user?.DefaultSiteId ?? 0;

        var (items, total) = await _repository.ListLinkedSitesAsync(userId, search, page, pageSize, cancellationToken);

        var dtos = items.Select(s => new UserSiteListItemDto
        {
            SiteId = s.Id,
            Code = s.Code,
            Name = s.Name,
            IsDefault = s.Id == defaultSiteId,
        }).ToList();

        return new PagedResult<UserSiteListItemDto>(dtos, total, page, pageSize);
    }

    public async Task<IReadOnlyList<AvailableSiteDto>> GetAvailableSitesAsync(
        string userId, CancellationToken cancellationToken = default)
    {
        var sites = await _repository.GetAvailableSitesAsync(userId, cancellationToken);
        return sites.Select(s => new AvailableSiteDto { Id = s.Id, Code = s.Code, Name = s.Name }).ToList();
    }

    public async Task AddAsync(AssociateSiteRequest request, CancellationToken cancellationToken = default)
    {
        await _associateValidator.ValidateAndThrowAsync(request, cancellationToken);
        await _repository.AddSiteLinkAsync(request.UserId, request.SiteId, cancellationToken);
    }

    public async Task<SiteUnlinkResult> RemoveAsync(
        string userId, int siteId, CancellationToken cancellationToken = default)
    {
        var user = await _repository.GetByIdAsync(userId, cancellationToken);
        if (user is null)
        {
            return SiteUnlinkResult.NotFound;
        }

        // A planta padrão não pode ser removida (trocar a padrão na edição do usuário antes).
        if (siteId == user.DefaultSiteId)
        {
            return SiteUnlinkResult.IsDefault;
        }

        if (!await _repository.IsSiteLinkedAsync(userId, siteId, cancellationToken))
        {
            return SiteUnlinkResult.NotFound;
        }

        await _repository.RemoveSiteLinkAsync(userId, siteId, cancellationToken);
        return SiteUnlinkResult.Removed;
    }
}
