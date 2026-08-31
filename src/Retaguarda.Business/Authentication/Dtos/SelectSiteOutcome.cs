namespace Retaguarda.Business.Authentication.Dtos;

// Resultado da etapa de seleção da planta. Em caso de sucesso, traz o par de tokens.
public enum SelectSiteStatus
{
    Success,
    InvalidToken,
    SiteNotAllowed,
}

public sealed class SelectSiteOutcome
{
    public SelectSiteStatus Status { get; }
    public AuthTokens? Tokens { get; }

    private SelectSiteOutcome(SelectSiteStatus status, AuthTokens? tokens)
    {
        Status = status;
        Tokens = tokens;
    }

    public static SelectSiteOutcome Success(AuthTokens tokens)
        => new(SelectSiteStatus.Success, tokens);

    public static SelectSiteOutcome InvalidToken()
        => new(SelectSiteStatus.InvalidToken, null);

    public static SelectSiteOutcome SiteNotAllowed()
        => new(SelectSiteStatus.SiteNotAllowed, null);
}
