namespace Retaguarda.Shared;

/// <summary>
/// Tipos de claim próprios da aplicação.
/// </summary>
public static class RetaguardaClaims
{
    // Planta ativa do usuário na sessão (gravada no cookie de autenticação após o login,
    // na tela de seleção de planta).
    public const string SiteId = "SiteId";

    // Nome da planta ativa exibido na casca da aplicação. Mantido junto do SiteId para
    // evitar consulta ao banco a cada renderização do topbar.
    public const string SiteName = "SiteName";
}
