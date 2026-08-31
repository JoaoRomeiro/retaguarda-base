using System.ComponentModel.DataAnnotations;

namespace Retaguarda.Web.Models.Account;

// Tela de redefinição de senha. Email e Code chegam pela URL do link enviado
// por e-mail e trafegam em campos ocultos; a política de senha (regex) reflete
// as regras do Identity configuradas no Program.cs.
public sealed class ResetPasswordViewModel
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    // Token de reset codificado em Base64Url (gerado no ForgotPassword).
    [Required]
    public string Code { get; set; } = string.Empty;

    [Required(ErrorMessage = "field_required")]
    [RegularExpression(
        @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^a-zA-Z0-9]).{8,}$",
        ErrorMessage = "password_policy")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Compare(nameof(Password), ErrorMessage = "password_mismatch")]
    [DataType(DataType.Password)]
    public string ConfirmPassword { get; set; } = string.Empty;
}
