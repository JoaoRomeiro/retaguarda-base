using System.Text.RegularExpressions;

namespace Retaguarda.UnitTests.Ui;

/// <summary>
/// Guarda o padrão de texto de ajuda em campos de formulário (`docs/padrao-ui.md` §8.2).
/// Documentação não impede o desvio; este teste impede — ele faz parte do gate
/// (`dotnet test -c Release`) e vale igual para quem escreve à mão e para agentes de IA.
///
/// Escopo assumido: é varredura textual sobre Razor, então pega os desvios óbvios (que são os
/// que acontecem na prática), não todo caso possível.
/// </summary>
public sealed class FieldHelpConventionTests
{
    private const string Doc = "Ver docs/padrao-ui.md §8.2.";

    /// <summary>Os partials que definem o padrão — eles próprios são a exceção às regras.</summary>
    private static readonly string[] PartialFileNames = ["_FieldHelp.cshtml", "_FieldHint.cshtml"];

    [Fact]
    public void Views_do_not_handwrite_form_text()
    {
        var offenders = Views()
            .Where(view => !PartialFileNames.Contains(Path.GetFileName(view.Path), StringComparer.Ordinal))
            .Where(view => view.Content.Contains("class=\"form-text\"", StringComparison.Ordinal))
            .Select(view => view.RelativePath)
            .ToList();

        Assert.True(
            offenders.Count == 0,
            $"Texto de ajuda visível deve usar <partial name=\"_FieldHint\" …>, nunca " +
            $"class=\"form-text\" escrito à mão — senão o aria-describedby fica de fora. {Doc}\n" +
            string.Join('\n', offenders));
    }

    [Fact]
    public void Fields_with_help_are_wired_by_aria_describedby()
    {
        // Casa `new FieldHelp("Campo"` e guarda o id do campo.
        var usage = new Regex("""new\s+FieldHelp\(\s*"(?<field>[^"]+)"\s*,""", RegexOptions.Compiled);
        var offenders = new List<string>();

        foreach (var view in Views().Where(v => !PartialFileNames.Contains(Path.GetFileName(v.Path), StringComparer.Ordinal)))
        {
            foreach (var fieldId in usage.Matches(view.Content).Select(m => m.Groups["field"].Value).Distinct(StringComparer.Ordinal))
            {
                // O campo pode montar o valor condicionalmente (`@(cond ? "Name-help" : null)`),
                // por isso a busca é pelo id e não pelo atributo inteiro.
                if (!view.Content.Contains($"\"{fieldId}-help\"", StringComparison.Ordinal))
                {
                    offenders.Add($"{view.RelativePath}: campo '{fieldId}' tem ajuda mas nenhum aria-describedby=\"{fieldId}-help\"");
                }
            }
        }

        Assert.True(
            offenders.Count == 0,
            $"Todo campo com texto de ajuda precisa referenciá-lo no aria-describedby. {Doc}\n" +
            string.Join('\n', offenders));
    }

    [Fact]
    public void Help_partials_stay_outside_the_label()
    {
        // O nome acessível do campo vem do conteúdo do <label>; ajuda lá dentro seria anunciada
        // como parte do rótulo em toda navegação. O lugar é o wrapper .form-label-row.
        var labels = new Regex("<label\\b[^>]*>.*?</label>", RegexOptions.Compiled | RegexOptions.Singleline);
        var offenders = new List<string>();

        foreach (var view in Views().Where(v => !PartialFileNames.Contains(Path.GetFileName(v.Path), StringComparer.Ordinal)))
        {
            if (labels.Matches(view.Content).Any(m =>
                    m.Value.Contains("_FieldHelp", StringComparison.Ordinal) ||
                    m.Value.Contains("_FieldHint", StringComparison.Ordinal)))
            {
                offenders.Add(view.RelativePath);
            }
        }

        Assert.True(
            offenders.Count == 0,
            $"_FieldHelp/_FieldHint não podem ficar dentro do <label> — use " +
            $"<div class=\"form-label-row\"> com o label e o partial lado a lado. {Doc}\n" +
            string.Join('\n', offenders));
    }

    [Fact]
    public void Views_folder_is_actually_found()
    {
        // Rede de segurança: se a varredura parar de achar arquivos (pasta movida, projeto
        // renomeado), os testes acima passariam vazios e o padrão deixaria de ser guardado.
        Assert.NotEmpty(Views());
    }

    private static List<RazorView> Views()
    {
        var viewsRoot = Path.Combine(RepositoryRoot(), "src", "Retaguarda.Web", "Views");

        return Directory.EnumerateFiles(viewsRoot, "*.cshtml", SearchOption.AllDirectories)
            .Select(path => new RazorView(
                path,
                Path.GetRelativePath(RepositoryRoot(), path).Replace('\\', '/'),
                File.ReadAllText(path)))
            .ToList();
    }

    /// <summary>Sobe a partir do binário até achar a solution — funciona em qualquer máquina.</summary>
    private static string RepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Retaguarda.sln")))
        {
            directory = directory.Parent;
        }

        Assert.NotNull(directory);
        return directory.FullName;
    }

    private sealed record RazorView(string Path, string RelativePath, string Content);
}
