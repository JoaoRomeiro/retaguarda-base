namespace Retaguarda.Shared.Authorization;

/// <summary>
/// Uma permissão do catálogo. O nome segue <c>recurso.acao</c> (ex.: <c>sites.edit</c>) e é
/// CONTRATO: uma vez publicado, fica gravado no banco de cada instalação, então renomear exige
/// migration. O escopo é global — a plataforma não tem permissão por planta (decisão de 2026-09-02).
/// </summary>
/// <param name="Name">Nome completo, ex.: <c>sites.edit</c>.</param>
/// <param name="Resource">Recurso ao qual pertence, ex.: <c>sites</c>. Usado para agrupar na tela.</param>
public sealed record PermissionDefinition(string Name, string Resource)
{
    /// <summary>
    /// Chave do rótulo no <c>SharedResources.pt-BR.resx</c>: <c>sites.edit</c> → <c>permission_sites_edit</c>.
    /// Derivada do nome de propósito — duplicar a chave no catálogo só criaria uma segunda fonte
    /// da verdade para manter em sincronia.
    /// </summary>
    public string ResourceKey => $"permission_{Name.Replace('.', '_')}";
}
