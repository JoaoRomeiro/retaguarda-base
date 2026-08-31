namespace Retaguarda.Data.Repositories;

/// <summary>
/// Configurações operacionais da planta ativa, definidas no CRUD de Planta. Hoje só o fuso de
/// exibição; ao acrescentar uma configuração por planta, some o campo aqui, na entidade
/// <c>Site</c> e no CRUD — o resto da aplicação continua lendo por este ponto único.
/// </summary>
public sealed record SiteSettings(string TimeZoneId)
{
    // Fuso usado quando não há planta ativa (ou a planta sumiu). Não é "o fuso do sistema": é só o
    // que sobra quando não há de quem herdar.
    public const string FallbackTimeZoneId = "America/Sao_Paulo";

    // Fallback quando não há planta ativa no contexto (ou a planta sumiu).
    public static SiteSettings Defaults { get; } = new(FallbackTimeZoneId);
}

public interface ISiteSettingsRepository
{
    // Configurações da planta ativa; cai nos defaults quando não há planta no contexto.
    Task<SiteSettings> GetAsync(CancellationToken cancellationToken = default);
}
