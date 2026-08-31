using Retaguarda.Business.Users.Dtos;

namespace Retaguarda.Web.Models.SiteSelection;

// Tela de seleção de planta ativa após o login (roadmap 2.2.1).
public sealed class SiteSelectionViewModel
{
    public int SiteId { get; set; }
    public string? ReturnUrl { get; set; }
    public IReadOnlyList<AvailableSiteDto> Sites { get; set; } = [];
}
