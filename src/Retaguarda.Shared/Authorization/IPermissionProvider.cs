namespace Retaguarda.Shared.Authorization;

/// <summary>
/// Fornece permissões ao catálogo. É o ponto de extensão da base: o projeto derivado registra o
/// próprio provider no DI e as permissões do domínio dele entram no catálogo sem editar nenhum
/// arquivo da plataforma.
/// </summary>
public interface IPermissionProvider
{
    IEnumerable<PermissionDefinition> GetPermissions();
}
