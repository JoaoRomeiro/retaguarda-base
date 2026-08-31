using Retaguarda.Business.Sites;
using Retaguarda.Data.Repositories;

namespace Retaguarda.UnitTests.Sites;

// Configurações da planta ativa em memória: permite testar os limiares e o período padrão sem banco.
internal sealed class FakeSiteSettingsService : ISiteSettingsService
{
    public SiteSettings Settings { get; set; } = SiteSettings.Defaults;

    public Task<SiteSettings> GetAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(Settings);
}
