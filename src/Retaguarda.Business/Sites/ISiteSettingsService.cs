using Retaguarda.Data.Repositories;

namespace Retaguarda.Business.Sites;

/// <summary>
/// Configurações operacionais da planta ativa (hoje, o fuso de exibição). Ponto único de leitura:
/// as telas e os serviços de consulta passam por aqui, em vez de cada um conversar direto com o
/// repositório.
/// </summary>
public interface ISiteSettingsService
{
    Task<SiteSettings> GetAsync(CancellationToken cancellationToken = default);
}
