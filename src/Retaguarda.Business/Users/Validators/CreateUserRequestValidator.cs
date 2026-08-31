using FluentValidation;
using Retaguarda.Business.Users.Dtos;
using Retaguarda.Data.Repositories;

namespace Retaguarda.Business.Users.Validators;

// As mensagens são CHAVES de recurso; a camada web/api as localiza via IStringLocalizer.
public sealed class CreateUserRequestValidator : AbstractValidator<CreateUserRequest>
{
    public CreateUserRequestValidator(IUserRepository repository)
    {
        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("user_fullname_required")
            .MaximumLength(200).WithMessage("user_fullname_too_long");

        RuleFor(x => x.Email)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("user_email_required")
            .EmailAddress().WithMessage("user_email_invalid")
            .MaximumLength(256).WithMessage("user_email_too_long")
            .MustAsync(async (email, ct) => !await repository.EmailExistsAsync(email, excludeId: null, ct))
                .WithMessage("user_email_in_use");

        // Política de senha alinhada às opções do Identity (len 8 + 4 classes de caracteres).
        RuleFor(x => x.Password)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("user_password_required")
            .MinimumLength(8).WithMessage("user_password_policy")
            .Matches("[A-Z]").WithMessage("user_password_policy")
            .Matches("[a-z]").WithMessage("user_password_policy")
            .Matches("[0-9]").WithMessage("user_password_policy")
            .Matches("[^a-zA-Z0-9]").WithMessage("user_password_policy");

        RuleFor(x => x.RoleName)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("user_role_required")
            .MustAsync(async (role, ct) => await repository.RoleExistsAsync(role, ct))
                .WithMessage("user_role_invalid");

        // A planta escolhida vira a padrão e a primeira associação.
        RuleFor(x => x.DefaultSiteId)
            .Cascade(CascadeMode.Stop)
            .GreaterThan(0).WithMessage("user_default_site_required")
            .MustAsync(async (id, ct) => await repository.SitesExistAsync([id], ct))
                .WithMessage("user_site_invalid");
    }
}
