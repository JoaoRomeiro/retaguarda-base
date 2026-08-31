using Retaguarda.Business.Authentication.Dtos;

namespace Retaguarda.Business.Authentication;

/// <summary>
/// Fluxo de autenticação da API em duas etapas: credenciais e, em seguida,
/// seleção obrigatória da planta ativa antes de emitir o access token.
/// </summary>
public interface IAuthenticationService
{
    // Etapa 1: valida credenciais e devolve o token de pré-autenticação + plantas disponíveis.
    Task<LoginOutcome> LoginAsync(string email, string password, CancellationToken cancellationToken = default);

    // Etapa 2: valida o token de pré-autenticação e a planta escolhida; emite access + refresh.
    Task<SelectSiteOutcome> SelectSiteAsync(
        string preAuthToken, int siteId, CancellationToken cancellationToken = default);

    // Renova o par de tokens (rotação): valida o refresh token, revoga-o e emite um novo par,
    // preservando a planta ativa e relendo os papéis atuais do usuário.
    Task<RefreshOutcome> RefreshAsync(string refreshToken, CancellationToken cancellationToken = default);

    // Revoga o refresh token informado (logout), desde que pertença ao usuário autenticado.
    Task<LogoutOutcome> LogoutAsync(
        string refreshToken, string userId, CancellationToken cancellationToken = default);
}
