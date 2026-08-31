namespace Retaguarda.Business.Sites;

/// <summary>
/// Lista curada de fusos horários do Brasil (IANA), cobrindo os quatro offsets do país.
/// Independente de SO (não usa GetSystemTimeZones, cujos IDs divergem Windows/Linux).
/// Os rótulos de exibição ficam no .resx (chave <see cref="Option.ResourceKey"/>).
/// </summary>
public static class BrazilTimeZones
{
    public sealed record Option(string Id, string ResourceKey);

    // Ordenados de leste (UTC−2) para oeste (UTC−5), com Brasília no topo.
    public static IReadOnlyList<Option> All { get; } =
    [
        new("America/Sao_Paulo", "tz_america_sao_paulo"),
        new("America/Fortaleza", "tz_america_fortaleza"),
        new("America/Recife", "tz_america_recife"),
        new("America/Belem", "tz_america_belem"),
        new("America/Bahia", "tz_america_bahia"),
        new("America/Manaus", "tz_america_manaus"),
        new("America/Cuiaba", "tz_america_cuiaba"),
        new("America/Boa_Vista", "tz_america_boa_vista"),
        new("America/Porto_Velho", "tz_america_porto_velho"),
        new("America/Rio_Branco", "tz_america_rio_branco"),
        new("America/Noronha", "tz_america_noronha"),
    ];

    private static readonly HashSet<string> Ids = All.Select(o => o.Id).ToHashSet(StringComparer.Ordinal);

    // True se o identificador pertence à lista curada (usado na validação).
    public static bool Contains(string? timeZoneId)
        => !string.IsNullOrWhiteSpace(timeZoneId) && Ids.Contains(timeZoneId);
}
