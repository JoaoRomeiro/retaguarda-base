using System.Text.RegularExpressions;

namespace Retaguarda.Shared.Authorization;

/// <summary>
/// Catálogo montado a partir dos <see cref="IPermissionProvider"/> registrados no DI.
///
/// Valida na construção e ESTOURA na subida da aplicação em caso de nome inválido ou duplicado.
/// É de propósito: permissão com nome errado não nega acesso de forma visível — ela simplesmente
/// nunca casa, e o problema só aparece quando um usuário reclama. Falhar no boot troca um bug
/// silencioso por um erro imediato.
/// </summary>
public sealed partial class PermissionCatalog : IPermissionCatalog
{
    public PermissionCatalog(IEnumerable<IPermissionProvider> providers)
    {
        ArgumentNullException.ThrowIfNull(providers);

        var permissions = providers.SelectMany(provider => provider.GetPermissions()).ToList();

        foreach (var permission in permissions)
        {
            if (!NamePattern().IsMatch(permission.Name))
            {
                throw new InvalidOperationException(
                    $"Permissão '{permission.Name}' fora do padrão 'recurso.acao' em minúsculas (ex.: sites.edit).");
            }

            if (!permission.Name.StartsWith(permission.Resource + ".", StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Permissão '{permission.Name}' não pertence ao recurso declarado '{permission.Resource}'.");
            }
        }

        var duplicates = permissions
            .GroupBy(permission => permission.Name, StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToList();

        if (duplicates.Count > 0)
        {
            throw new InvalidOperationException(
                $"Permissões duplicadas no catálogo: {string.Join(", ", duplicates)}.");
        }

        // Ordem de declaração, não alfabética: é ela que carrega a intenção. Alfabética renderiza
        // "Criar, Excluir, Editar, Listar" na tela; declarada renderiza "Listar, Criar, Editar,
        // Excluir, Exportar" — o ciclo de vida do cadastro. Vale também entre recursos, e assim
        // cada projeto derivado controla a ordem dos dele.
        All = permissions;

        ByResource = All
            .GroupBy(permission => permission.Resource, StringComparer.Ordinal)
            .ToList();

        _names = All.Select(permission => permission.Name).ToHashSet(StringComparer.Ordinal);
    }

    private readonly HashSet<string> _names;

    public IReadOnlyList<PermissionDefinition> All { get; }

    public IReadOnlyList<IGrouping<string, PermissionDefinition>> ByResource { get; }

    public bool Contains(string permission) =>
        !string.IsNullOrWhiteSpace(permission) && _names.Contains(permission);

    // 'recurso.acao', podendo ter mais níveis (users.sites.manage). Só minúsculas e dígitos.
    [GeneratedRegex(@"^[a-z][a-z0-9]*(\.[a-z][a-z0-9]*)+$")]
    private static partial Regex NamePattern();
}
