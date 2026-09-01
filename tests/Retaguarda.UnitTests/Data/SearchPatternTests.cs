using Retaguarda.Data.Repositories;

namespace Retaguarda.UnitTests.Data;

// O termo de busca vem do usuário e vira padrão de ILIKE. Sem escape, "%" traz a tabela inteira
// e "_" casa com qualquer caractere — resultado errado, não injeção (o valor segue parametrizado).
public sealed class SearchPatternTests
{
    [Fact]
    public void Wraps_the_term_for_a_contains_search()
        => Assert.Equal("%matriz%", SearchPattern.Contains("matriz"));

    [Fact]
    public void Trims_the_typed_term()
        => Assert.Equal("%matriz%", SearchPattern.Contains("  matriz  "));

    [Fact]
    public void Escapes_the_percent_wildcard()
    {
        // Sem escape isto viraria "%%%", que casa com qualquer registro.
        Assert.Equal("%\\%%", SearchPattern.Contains("%"));
        Assert.Equal("%10\\% off%", SearchPattern.Contains("10% off"));
    }

    [Fact]
    public void Escapes_the_underscore_wildcard()
    {
        // "_" no ILIKE casa com um caractere qualquer; aqui precisa valer como texto.
        Assert.Equal("%a\\_b%", SearchPattern.Contains("a_b"));
    }

    [Fact]
    public void Escapes_the_escape_character_itself()
    {
        // A barra é escapada primeiro; se fosse por último, dobraria as barras do próprio escape.
        Assert.Equal("%c:\\\\temp%", SearchPattern.Contains(@"c:\temp"));
    }

    [Fact]
    public void Escapes_all_wildcards_combined()
        => Assert.Equal("%\\\\\\%\\_%", SearchPattern.Contains(@"\%_"));

    [Fact]
    public void Keeps_an_empty_term_harmless()
        => Assert.Equal("%%", SearchPattern.Contains("   "));
}
