using System.Text.RegularExpressions;
using Retaguarda.Shared.Authorization;

namespace Retaguarda.UnitTests.Authorization;

/// <summary>
/// Impede permissão fantasma: uma string digitada errada em <c>[Authorize(Policy = "...")]</c> não
/// gera erro nenhum em runtime — o acesso simplesmente é negado para todo mundo, e ninguém descobre
/// até alguém reclamar. Aqui o build quebra na hora.
/// </summary>
public sealed partial class PermissionConventionTests
{
    private static readonly PermissionCatalog Catalog = new([new PlatformPermissions.Provider()]);

    [Fact]
    public void Every_policy_used_in_code_exists_in_the_catalog()
    {
        var offenders = new List<string>();

        foreach (var file in SourceFiles())
        {
            var content = File.ReadAllText(file.Path);

            foreach (Match match in PolicyLiteral().Matches(content))
            {
                var policy = match.Groups["policy"].Value;

                // Só cobramos os nomes que PARECEM permissão (recurso.acao). Uma política nomeada
                // comum, se um dia existir, não cai nesta regra.
                if (PermissionShape().IsMatch(policy) && !Catalog.Contains(policy))
                {
                    offenders.Add($"{file.RelativePath}: policy \"{policy}\" não existe no catálogo de permissões");
                }
            }
        }

        Assert.True(
            offenders.Count == 0,
            "Toda permissão usada em [Authorize(Policy = ...)] precisa existir em um IPermissionProvider. " +
            "Prefira as constantes de PlatformPermissions a string solta.\n" + string.Join('\n', offenders));
    }

    [Fact]
    public void Catalog_names_follow_the_resource_action_pattern()
    {
        var offenders = Catalog.All
            .Where(permission => !PermissionShape().IsMatch(permission.Name))
            .Select(permission => permission.Name)
            .ToList();

        Assert.True(offenders.Count == 0, string.Join('\n', offenders));
    }

    [Fact]
    public void Source_scan_actually_finds_files()
    {
        // Rede de segurança: sem isso, uma pasta movida faria o teste acima passar vazio.
        Assert.NotEmpty(SourceFiles());
    }

    private static List<SourceFile> SourceFiles()
    {
        var root = Path.Combine(RepositoryRoot(), "src");

        return Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories)
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Select(path => new SourceFile(path, Path.GetRelativePath(RepositoryRoot(), path).Replace('\\', '/')))
            .ToList();
    }

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

    // Casa apenas o literal: [Authorize(Policy = "sites.edit")]. O uso por constante
    // (PlatformPermissions.Sites.Edit) já é garantido pelo compilador.
    [GeneratedRegex("Policy\\s*=\\s*\"(?<policy>[^\"]*)\"", RegexOptions.Compiled)]
    private static partial Regex PolicyLiteral();

    [GeneratedRegex(@"^[a-z][a-z0-9]*(\.[a-z][a-z0-9]*)+$", RegexOptions.Compiled)]
    private static partial Regex PermissionShape();

    private sealed record SourceFile(string Path, string RelativePath);
}
