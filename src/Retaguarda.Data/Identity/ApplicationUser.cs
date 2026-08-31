using Microsoft.AspNetCore.Identity;
using Retaguarda.Data.Entities;
using Retaguarda.Shared.Contracts;

namespace Retaguarda.Data.Identity;

// Usuário da aplicação (operador ou gestor). Acesso a 1..N plantas via SiteLinks,
// com uma planta padrão (DefaultSiteId) que entra ativa no login (§4.4/§5.1).
public class ApplicationUser : IdentityUser, IAuditable, ISoftDeletable
{
    // Nome completo exibido na UI.
    public string FullName { get; set; } = string.Empty;

    // Usuário ativo? Inativos não conseguem autenticar (ver ApplicationSignInManager).
    public bool IsActive { get; set; } = true;

    // Idioma preferido (§12.6/§710): preparado, sem UI de troca por enquanto.
    public string? PreferredLanguage { get; set; }

    // Planta padrão (entra ativa no login). Deve estar entre as plantas vinculadas (SiteLinks);
    // a regra é garantida no serviço, não no banco.
    public int DefaultSiteId { get; set; }
    public Site? DefaultSite { get; set; }

    // Plantas que o usuário pode acessar (N:N via UserSite).
    public ICollection<UserSite> SiteLinks { get; set; } = [];

    // Auditoria (§6.2) — carimbada automaticamente pelo AuditableEntityInterceptor.
    public DateTime CreatedAt { get; set; }
    public string? CreatedById { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedById { get; set; }

    // Soft delete (§6.2).
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string? DeletedById { get; set; }
}
