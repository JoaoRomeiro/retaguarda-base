using Microsoft.AspNetCore.Identity;
using Retaguarda.Shared.Contracts;

namespace Retaguarda.Data.Identity;

/// <summary>
/// Papel (grupo de permissão) do Identity, estendido com descrição, marcação de
/// papel interno e a mesma trilha de auditoria + soft delete dos cadastros (§5.2).
/// Permanece na tabela [identity].[Roles] — NÃO é uma tabela nova. A PK continua
/// string/GUID, herdada do IdentityRole (convenção do Identity). Escopo global:
/// papéis NÃO possuem SiteId (autorização é system-wide — §2.4).
/// </summary>
public class ApplicationRole : IdentityRole, IAuditable, ISoftDeletable
{
    public ApplicationRole()
    {
    }

    public ApplicationRole(string roleName) : base(roleName)
    {
    }

    // Descrição amigável exibida na UI (mapeia a coluna "Definição" do §2.4).
    public string? Description { get; set; }

    // Marca os papéis internos pré-cadastrados (Admin, Manager, Picker, Inspector):
    // protegidos contra renomeação/exclusão, pois o código e [Authorize(Roles=...)]
    // dependem dos nomes exatos.
    public bool IsSystem { get; set; }

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
