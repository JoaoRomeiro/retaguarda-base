using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Retaguarda.Data.Identity;

namespace Retaguarda.Web.Infrastructure;

// SignInManager customizado: além das checagens padrão do Identity (e-mail confirmado,
// lockout), recusa o login de usuários inativos (IsActive == false). Usuários excluídos
// logicamente já são filtrados pelo global query filter, então nem são encontrados.
public sealed class ApplicationSignInManager : SignInManager<ApplicationUser>
{
    public ApplicationSignInManager(
        UserManager<ApplicationUser> userManager,
        IHttpContextAccessor contextAccessor,
        IUserClaimsPrincipalFactory<ApplicationUser> claimsFactory,
        IOptions<IdentityOptions> optionsAccessor,
        ILogger<SignInManager<ApplicationUser>> logger,
        IAuthenticationSchemeProvider schemes,
        IUserConfirmation<ApplicationUser> confirmation)
        : base(userManager, contextAccessor, claimsFactory, optionsAccessor, logger, schemes, confirmation)
    {
    }

    public override async Task<bool> CanSignInAsync(ApplicationUser user)
    {
        if (!user.IsActive)
        {
            Logger.LogWarning("Sign-in blocked for inactive user {UserId}.", user.Id);
            return false;
        }

        return await base.CanSignInAsync(user);
    }
}
