namespace Retaguarda.Business.Authentication.Dtos;

// Resultado do logout. NotFound cobre token inexistente, já revogado/expirado ou que não
// pertence ao usuário autenticado (não distinguir evita vazar a existência de tokens alheios).
public enum LogoutStatus
{
    Success,
    NotFound,
}

public sealed class LogoutOutcome
{
    public LogoutStatus Status { get; }

    private LogoutOutcome(LogoutStatus status) => Status = status;

    public static LogoutOutcome Success() => new(LogoutStatus.Success);

    public static LogoutOutcome NotFound() => new(LogoutStatus.NotFound);
}
