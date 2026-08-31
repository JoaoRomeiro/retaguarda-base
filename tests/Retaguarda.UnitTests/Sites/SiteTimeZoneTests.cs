using Retaguarda.Data.Repositories;
using Retaguarda.Web.Infrastructure;

namespace Retaguarda.UnitTests.Sites;

// O fuso de exibição vem da planta ativa (campo do CRUD de Plantas). O banco guarda UTC; a
// conversão acontece aqui, na borda da apresentação.
public sealed class SiteTimeZoneTests
{
    // 12:00 UTC — São Paulo (UTC-3) vê 09:00; Manaus (UTC-4) vê 08:00.
    private static readonly DateTime NoonUtc = new(2026, 8, 12, 12, 0, 0, DateTimeKind.Utc);

    private static SiteTimeZone Build(string timeZoneId)
    {
        var settings = new FakeSiteSettingsService
        {
            Settings = SiteSettings.Defaults with { TimeZoneId = timeZoneId },
        };

        return new SiteTimeZone(settings);
    }

    [Fact]
    public async Task ToLocal_uses_the_time_zone_of_the_active_site()
    {
        var timeZone = Build("America/Sao_Paulo");
        await timeZone.ResolveAsync();

        Assert.Equal(new DateTime(2026, 8, 12, 9, 0, 0), timeZone.ToLocal(NoonUtc));
    }

    [Fact]
    public async Task Another_site_sees_its_own_hour()
    {
        var timeZone = Build("America/Manaus");
        await timeZone.ResolveAsync();

        Assert.Equal(new DateTime(2026, 8, 12, 8, 0, 0), timeZone.ToLocal(NoonUtc));
    }

    [Fact]
    public async Task ToUtc_is_the_inverse_of_ToLocal()
    {
        var timeZone = Build("America/Manaus");
        await timeZone.ResolveAsync();

        var local = timeZone.ToLocal(NoonUtc);

        Assert.Equal(NoonUtc, DateTime.SpecifyKind(timeZone.ToUtc(local), DateTimeKind.Utc));
    }

    [Fact]
    public async Task An_unknown_time_zone_falls_back_instead_of_breaking_the_screen()
    {
        // O cadastro só aceita fusos de uma lista curada; isto cobre dado legado ou editado à mão.
        var timeZone = Build("Nao/Existe");
        await timeZone.ResolveAsync();

        Assert.Equal(new DateTime(2026, 8, 12, 9, 0, 0), timeZone.ToLocal(NoonUtc));
    }

    [Fact]
    public void Without_an_active_site_the_fallback_zone_is_used()
    {
        // Login e seleção de planta rodam sem planta ativa: a tela ainda precisa mostrar horas.
        var timeZone = Build("America/Manaus");  // sem ResolveAsync

        Assert.Equal(new DateTime(2026, 8, 12, 9, 0, 0), timeZone.ToLocal(NoonUtc));
    }
}
