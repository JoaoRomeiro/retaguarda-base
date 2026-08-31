using Retaguarda.Business.Sites;
using Retaguarda.Data.Repositories;

namespace Retaguarda.Web.Infrastructure;

/// <summary>
/// Fuso de exibição da PLANTA ATIVA. Toda data mostrada ou digitada nas telas passa por aqui:
/// o banco guarda UTC, a tela mostra a hora local da planta.
///
/// Escopo de requisição: o fuso é resolvido uma vez, pelo <see cref="SiteTimeZoneFilter"/>, antes
/// da action rodar — as views precisam de acesso síncrono e não podem esperar uma consulta.
/// Sem planta ativa (login, seleção de planta), cai no fuso de fallback.
/// </summary>
public sealed class SiteTimeZone
{
    private readonly ISiteSettingsService _settings;
    private TimeZoneInfo? _zone;

    public SiteTimeZone(ISiteSettingsService settings) => _settings = settings;

    /// <summary>Fuso resolvido da planta ativa; o fallback vale enquanto ninguém resolveu.</summary>
    public TimeZoneInfo Zone => _zone ??= Find(SiteSettings.FallbackTimeZoneId);

    /// <summary>Lê o fuso da planta ativa. Chamado uma vez por requisição, pelo filtro.</summary>
    public async Task ResolveAsync(CancellationToken cancellationToken = default)
    {
        var settings = await _settings.GetAsync(cancellationToken);
        _zone = Find(settings.TimeZoneId);
    }

    /// <summary>Converte um instante UTC para a hora local da planta (Kind = Unspecified).</summary>
    public DateTime ToLocal(DateTime utc)
        => TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Unspecified), Zone);

    /// <summary>Converte uma hora local da planta para UTC (Kind = Utc).</summary>
    public DateTime ToUtc(DateTime local)
        => TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(local, DateTimeKind.Unspecified), Zone);

    // Id inválido nunca derruba a tela: cai no fallback e, se nem ele existir no SO, em UTC. O
    // cadastro só aceita fusos de uma lista curada, então isto é rede de proteção, não caminho normal.
    private static TimeZoneInfo Find(string id)
    {
        if (TimeZoneInfo.TryFindSystemTimeZoneById(id, out var zone))
        {
            return zone;
        }

        return TimeZoneInfo.TryFindSystemTimeZoneById(SiteSettings.FallbackTimeZoneId, out var fallback)
            ? fallback
            : TimeZoneInfo.Utc;
    }
}
