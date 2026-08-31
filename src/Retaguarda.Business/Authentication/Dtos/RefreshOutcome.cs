namespace Retaguarda.Business.Authentication.Dtos;

// Resultado da renovação de tokens. Invalid cobre token inexistente, expirado, já revogado
// (rotacionado) ou cujo usuário/planta deixou de ser válido — mensagem neutra ao cliente.
public enum RefreshStatus
{
    Success,
    Invalid,
}

public sealed class RefreshOutcome
{
    public RefreshStatus Status { get; }
    public AuthTokens? Tokens { get; }

    private RefreshOutcome(RefreshStatus status, AuthTokens? tokens)
    {
        Status = status;
        Tokens = tokens;
    }

    public static RefreshOutcome Success(AuthTokens tokens)
        => new(RefreshStatus.Success, tokens);

    public static RefreshOutcome Invalid()
        => new(RefreshStatus.Invalid, null);
}
