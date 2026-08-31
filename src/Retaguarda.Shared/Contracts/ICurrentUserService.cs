namespace Retaguarda.Shared.Contracts;

/// <summary>
/// Expõe o usuário e o site correntes a partir do contexto da requisição
/// (cookie no Web, JWT no Api). A implementação vive na camada web
/// (Retaguarda.AspNetCore), mantendo Shared agnóstico de framework (§4.2).
/// </summary>
public interface ICurrentUserService
{
    /// <summary>Id do usuário autenticado (AspNetUsers.Id) ou null se anônimo.</summary>
    string? UserId { get; }

    /// <summary>Site corrente do usuário, quando aplicável (isolamento multi-site, §4.4).</summary>
    int? SiteId { get; }
}
