namespace Retaguarda.Shared.Authorization;

/// <summary>
/// Todas as permissões conhecidas pela aplicação, reunidas a partir dos <see cref="IPermissionProvider"/>
/// registrados. É a lista que alimenta os checkboxes do cadastro de Acessos e que decide se uma
/// política de autorização existe.
/// </summary>
public interface IPermissionCatalog
{
    /// <summary>Todas as permissões, ordenadas por recurso e nome.</summary>
    IReadOnlyList<PermissionDefinition> All { get; }

    /// <summary>Permissões agrupadas por recurso, na ordem em que devem aparecer na tela.</summary>
    IReadOnlyList<IGrouping<string, PermissionDefinition>> ByResource { get; }

    bool Contains(string permission);
}
