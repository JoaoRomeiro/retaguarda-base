using Retaguarda.Data.Identity;

namespace Retaguarda.Data.Entities;

/// <summary>
/// Refresh token da API. Persistido como hash (nunca o valor bruto), vinculado
/// ao usuário e à planta ativa da sessão para que a renovação preserve o contexto multi-site.
/// Não é entidade auditável nem multi-site: é consultado antes de existir planta ativa.
/// </summary>
public sealed class RefreshToken
{
    public long Id { get; set; }

    // Hash (SHA-256, Base64) do token bruto entregue ao cliente. O bruto não é armazenado.
    public string TokenHash { get; set; } = string.Empty;

    // Usuário dono do token (FK para [identity].[Users]).
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser? User { get; set; }

    // Planta ativa fixada na sessão: a renovação reemite o access token para esta planta.
    public int SiteId { get; set; }

    // Expiração (UTC). Após esta data o token não renova mais.
    public DateTime ExpiresAt { get; set; }

    // Emissão (UTC).
    public DateTime CreatedAt { get; set; }

    // Revogação (UTC): preenchida no logout ou na rotação. Null enquanto ativo.
    public DateTime? RevokedAt { get; set; }
}
