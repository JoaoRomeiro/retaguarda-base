using Retaguarda.Business.Users.Dtos;

namespace Retaguarda.Business.Authentication.Dtos;

// Resultado da etapa de credenciais (login). Não emite access token ainda: devolve o
// token de pré-autenticação e as plantas selecionáveis para a segunda etapa.
public enum LoginStatus
{
    Success,
    InvalidCredentials,
    LockedOut,
}

public sealed class LoginOutcome
{
    public LoginStatus Status { get; }
    public string? PreAuthToken { get; }
    public IReadOnlyList<AvailableSiteDto> Sites { get; }
    public int DefaultSiteId { get; }

    private LoginOutcome(
        LoginStatus status, string? preAuthToken, IReadOnlyList<AvailableSiteDto> sites, int defaultSiteId)
    {
        Status = status;
        PreAuthToken = preAuthToken;
        Sites = sites;
        DefaultSiteId = defaultSiteId;
    }

    public static LoginOutcome Success(
        string preAuthToken, IReadOnlyList<AvailableSiteDto> sites, int defaultSiteId)
        => new(LoginStatus.Success, preAuthToken, sites, defaultSiteId);

    public static LoginOutcome InvalidCredentials()
        => new(LoginStatus.InvalidCredentials, null, [], 0);

    public static LoginOutcome LockedOut()
        => new(LoginStatus.LockedOut, null, [], 0);
}
