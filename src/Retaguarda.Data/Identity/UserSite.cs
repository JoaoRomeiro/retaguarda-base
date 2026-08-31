using Retaguarda.Data.Entities;

namespace Retaguarda.Data.Identity;

/// <summary>
/// Vínculo N:N entre usuário e planta (Site): as plantas que o usuário pode acessar.
/// A planta padrão fica em <see cref="ApplicationUser.DefaultSiteId"/> e deve estar
/// entre estes vínculos (regra garantida no serviço). Tabela [identity].[UserSites].
/// </summary>
public class UserSite
{
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;

    public int SiteId { get; set; }
    public Site Site { get; set; } = null!;
}
