using Retaguarda.Data.Repositories;

namespace Retaguarda.Business.Sites;

// Cache por requisição: numa mesma requisição várias camadas pedem as configurações e o valor não
// muda no meio dela. O serviço é registrado como Scoped.
public sealed class SiteSettingsService : ISiteSettingsService
{
    private readonly ISiteSettingsRepository _repository;
    private SiteSettings? _cached;

    public SiteSettingsService(ISiteSettingsRepository repository) => _repository = repository;

    public async Task<SiteSettings> GetAsync(CancellationToken cancellationToken = default)
        => _cached ??= await _repository.GetAsync(cancellationToken);
}
