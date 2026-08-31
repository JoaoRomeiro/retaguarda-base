using System.ComponentModel.DataAnnotations;

namespace Retaguarda.Web.Models.Account;

// Tela "Esqueci minha senha": o usuário informa o e-mail para receber o link.
public sealed class ForgotPasswordViewModel
{
    [Required(ErrorMessage = "field_required")]
    [EmailAddress(ErrorMessage = "invalid_email")]
    [DataType(DataType.EmailAddress)]
    public string Email { get; set; } = string.Empty;
}
