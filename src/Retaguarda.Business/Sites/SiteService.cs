using FluentValidation;
using Mapster;
using Retaguarda.Business.Sites.Dtos;
using Retaguarda.Data.Entities;
using Retaguarda.Data.Repositories;
using Retaguarda.Shared.Models;

namespace Retaguarda.Business.Sites;

public sealed class SiteService : ISiteService
{
    private const int DefaultPageSize = 20;

    private readonly ISiteRepository _repository;
    private readonly IValidator<CreateSiteRequest> _createValidator;
    private readonly IValidator<UpdateSiteRequest> _updateValidator;

    public SiteService(
        ISiteRepository repository,
        IValidator<CreateSiteRequest> createValidator,
        IValidator<UpdateSiteRequest> updateValidator)
    {
        _repository = repository;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    public async Task<PagedResult<SiteListItemDto>> ListAsync(
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
        var dtos = items.Adapt<List<SiteListItemDto>>();
        return new PagedResult<SiteListItemDto>(dtos, total, page, pageSize);
    }

    public async Task<SiteDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var site = await _repository.GetByIdAsync(id, cancellationToken);
        return site?.Adapt<SiteDto>();
    }

    public async Task<SiteDto> CreateAsync(CreateSiteRequest request, CancellationToken cancellationToken = default)
    {
        await _createValidator.ValidateAndThrowAsync(request, cancellationToken);

        var site = request.Adapt<Site>();
        var created = await _repository.AddAsync(site, cancellationToken);
        return created.Adapt<SiteDto>();
    }

    public async Task<bool> UpdateAsync(UpdateSiteRequest request, CancellationToken cancellationToken = default)
    {
        var site = await _repository.GetByIdAsync(request.Id, cancellationToken);
        if (site is null)
        {
            return false;
        }

        await _updateValidator.ValidateAndThrowAsync(request, cancellationToken);

        // Aplica as alterações do request sobre a entidade rastreada (audit/soft-delete intactos).
        request.Adapt(site);
        await _repository.UpdateAsync(site, cancellationToken);
        return true;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var site = await _repository.GetByIdAsync(id, cancellationToken);
        if (site is null)
        {
            return false;
        }

        await _repository.DeleteAsync(site, cancellationToken);
        return true;
    }
}
