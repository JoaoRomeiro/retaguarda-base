using System.ComponentModel.DataAnnotations;

namespace Retaguarda.Web.Models.Account;

// Dados do formulário de login. As mensagens de validação usam chaves resolvidas via
// .resx (DataAnnotationLocalizerProvider aponta para SharedResources). Labels ficam na view.
public sealed class LoginViewModel
{
    [Required(ErrorMessage = "field_required")]
    [EmailAddress(ErrorMessage = "invalid_email")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "field_required")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    public bool RememberMe { get; set; }

    // Destino pós-login; validado contra open redirect no controller.
    public string? ReturnUrl { get; set; }
}
