using Retaguarda.Business.Users.Dtos;

namespace Retaguarda.Business.Users;

/// <summary>
/// Seleção da planta ativa no login (roadmap 2.2.1). Só plantas ativas e associadas ao
/// usuário podem ser escolhidas.
/// </summary>
public interface ISiteSelectionService
{
    // Plantas que o usuário pode escolher como ativa (ativas + associadas).
    Task<IReadOnlyList<AvailableSiteDto>> GetSelectableSitesAsync(
        string userId, CancellationToken cancellationToken = default);

    // True se a planta é ativa e está associada ao usuário (validação da escolha).
    Task<bool> IsSelectableAsync(string userId, int siteId, CancellationToken cancellationToken = default);
}
