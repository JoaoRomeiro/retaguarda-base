using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Localization;
using Retaguarda.AspNetCore.Security;
using Retaguarda.Business.Notifications;
using Retaguarda.Data.Identity;
using Retaguarda.Shared;
using Retaguarda.Web.Infrastructure;
using Retaguarda.Web.Models.Account;

namespace Retaguarda.Web.Controllers;

// Autenticação por cookie + recuperação de senha (etapas 2.1a/2.1b).
// Por padrão exige autenticação; ações públicas são marcadas com [AllowAnonymous].
[Authorize]
public sealed class AccountController : Controller
{
    // Duração do cookie quando "Manter-me conectado" está marcado. Desmarcado, vale o
    // ExpireTimeSpan global (8h, idle) e o cookie morre ao fechar o navegador. Ver Program.cs.
    private const int RememberMeDays = 7;

    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IEmailSender _emailSender;
    private readonly IStringLocalizer<SharedResources> _localizer;
    private readonly IStringLocalizer<Emails> _emailLocalizer;
    private readonly ILogger<AccountController> _logger;

    public AccountController(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        IEmailSender emailSender,
        IStringLocalizer<SharedResources> localizer,
        IStringLocalizer<Emails> emailLocalizer,
        ILogger<AccountController> logger)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _emailSender = emailSender;
        _localizer = localizer;
        _emailLocalizer = emailLocalizer;
        _logger = logger;
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        if (_signInManager.IsSignedIn(User))
        {
            return RedirectToAction("Index", "Home");
        }

        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting(AuthRateLimiting.CredentialsPolicy)]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        // Valida a senha (com lockout) e assina em passos separados para controlar a duração do
        // cookie: o PasswordSignInAsync fixaria o ExpireTimeSpan global (8h), sem como distinguir
        // "manter conectado". Ver Program.cs.
        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user is not null)
        {
            var check = await _signInManager.CheckPasswordSignInAsync(user, model.Password, lockoutOnFailure: true);
            if (check.Succeeded)
            {
                // Marcado: cookie persistente de 7 dias (sobrevive a fechar o navegador).
                // Desmarcado: cookie de sessão, sob a expiração de 8h do ExpireTimeSpan.
                var properties = new AuthenticationProperties { IsPersistent = model.RememberMe };
                if (model.RememberMe)
                {
                    properties.ExpiresUtc = DateTimeOffset.UtcNow.AddDays(RememberMeDays);
                    properties.AllowRefresh = true;
                }

                await _signInManager.SignInAsync(user, properties);
                _logger.LogInformation("User {UserId} signed in", user.Id);
                return RedirectToLocal(model.ReturnUrl);
            }

            if (check.IsLockedOut)
            {
                _logger.LogWarning("User {UserId} locked out", user.Id);
            }
        }

        // Mensagem neutra: não revela se o e-mail existe (anti-enumeração).
        ModelState.AddModelError(string.Empty, _localizer["login_invalid"]);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        _logger.LogInformation("User signed out");
        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public async Task<IActionResult> Profile()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return Forbid();
        }

        return View(await BuildProfileViewModelAsync(user));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ProfileViewModel model)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return Forbid();
        }

        if (!ModelState.IsValid)
        {
            return View(nameof(Profile), await BuildProfileViewModelAsync(user, model));
        }

        var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
        if (!result.Succeeded)
        {
            AddPasswordChangeErrors(result);
            return View(nameof(Profile), await BuildProfileViewModelAsync(user, model));
        }

        await RefreshSignInPreservingActiveSiteAsync(user);
        _logger.LogInformation("User {UserId} changed own password", user.Id);
        TempData["StatusMessage"] = _localizer["profile_password_updated"].Value;

        return RedirectToAction(nameof(Profile));
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult AccessDenied() => View();

    // ---- Recuperação de senha (etapa 2.1b) -----------------------

    [HttpGet]
    [AllowAnonymous]
    public IActionResult ForgotPassword() => View(new ForgotPasswordViewModel());

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting(AuthRateLimiting.CredentialsPolicy)]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user is not null)
        {
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            // Token contém caracteres não seguros para URL → Base64Url.
            var code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
            var resetUrl = Url.Action(
                nameof(ResetPassword), "Account",
                new { email = model.Email, code },
                protocol: Request.Scheme);

            if (resetUrl is not null)
            {
                var subject = _emailLocalizer["password_reset_subject"].Value;
                var body = string.Format(
                    CultureInfo.CurrentCulture,
                    _emailLocalizer["password_reset_body"].Value,
                    resetUrl);

                await _emailSender.SendEmailAsync(model.Email, subject, body);
                _logger.LogInformation("Password reset link sent for user {UserId}", user.Id);
            }
        }
        else
        {
            // Mesma resposta para e-mail inexistente (anti-enumeração).
            _logger.LogInformation("Password reset requested for an unknown email");
        }

        return RedirectToAction(nameof(ForgotPasswordConfirmation));
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult ForgotPasswordConfirmation() => View();

    [HttpGet]
    [AllowAnonymous]
    public IActionResult ResetPassword(string? email = null, string? code = null)
    {
        // Link incompleto/inválido → volta para a solicitação.
        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(code))
        {
            return RedirectToAction(nameof(ForgotPassword));
        }

        return View(new ResetPasswordViewModel { Email = email, Code = code });
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting(AuthRateLimiting.CredentialsPolicy)]
    public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user is null)
        {
            // E-mail inexistente: mesma confirmação (anti-enumeração).
            return RedirectToAction(nameof(ResetPasswordConfirmation));
        }

        string token;
        try
        {
            token = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(model.Code));
        }
        catch (FormatException)
        {
            // Code malformado: trata como link inválido, sem detalhes.
            ModelState.AddModelError(string.Empty, _localizer["reset_link_invalid"]);
            return View(model);
        }

        var result = await _userManager.ResetPasswordAsync(user, token, model.Password);
        if (result.Succeeded)
        {
            _logger.LogInformation("Password reset succeeded for user {UserId}", user.Id);
            return RedirectToAction(nameof(ResetPasswordConfirmation));
        }

        // Token inválido/expirado → não revelar detalhes; pedir novo link.
        if (result.Errors.Any(e => e.Code.Contains("Token", StringComparison.OrdinalIgnoreCase)))
        {
            ModelState.AddModelError(string.Empty, _localizer["reset_link_invalid"]);
        }
        else
        {
            // Demais falhas (ex.: política de senha) — mensagem localizada genérica.
            ModelState.AddModelError(string.Empty, _localizer["reset_password_failed"]);
        }

        return View(model);
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult ResetPasswordConfirmation() => View();

    private async Task<ProfileViewModel> BuildProfileViewModelAsync(
        ApplicationUser user,
        ProfileViewModel? passwordModel = null)
    {
        var roles = await _userManager.GetRolesAsync(user);

        return new ProfileViewModel
        {
            FullName = user.FullName,
            Email = user.Email ?? string.Empty,
            RoleName = roles.Count == 0
                ? _localizer["profile_role_empty"].Value
                : string.Join(", ", roles),
            CurrentPassword = passwordModel?.CurrentPassword ?? string.Empty,
            NewPassword = passwordModel?.NewPassword ?? string.Empty,
            ConfirmPassword = passwordModel?.ConfirmPassword ?? string.Empty,
        };
    }

    private async Task RefreshSignInPreservingActiveSiteAsync(ApplicationUser user)
    {
        var authResult = await HttpContext.AuthenticateAsync(IdentityConstants.ApplicationScheme);
        var refreshedPrincipal = await _signInManager.CreateUserPrincipalAsync(user);
        ActiveSiteClaims.PreserveFromCurrentPrincipal(User, refreshedPrincipal);

        await HttpContext.SignInAsync(
            IdentityConstants.ApplicationScheme,
            refreshedPrincipal,
            authResult.Properties ?? new AuthenticationProperties());
    }

    private void AddPasswordChangeErrors(IdentityResult result)
    {
        foreach (var error in result.Errors)
        {
            if (string.Equals(error.Code, "PasswordMismatch", StringComparison.Ordinal))
            {
                ModelState.AddModelError(
                    nameof(ProfileViewModel.CurrentPassword),
                    _localizer["change_password_current_invalid"].Value);
                continue;
            }

            if (error.Code.StartsWith("Password", StringComparison.Ordinal))
            {
                ModelState.AddModelError(
                    nameof(ProfileViewModel.NewPassword),
                    _localizer["password_policy"].Value);
                continue;
            }

            ModelState.AddModelError(string.Empty, _localizer["change_password_failed"].Value);
        }
    }

    // Evita open redirect: só aceita URLs locais.
    private IActionResult RedirectToLocal(string? returnUrl)
    {
        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction("Index", "Home");
    }
}
