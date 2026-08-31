using FluentValidation;
using Retaguarda.Business.Users.Dtos;
using Retaguarda.Data.Repositories;

namespace Retaguarda.Business.Users.Validators;

public sealed class UpdateUserRequestValidator : AbstractValidator<UpdateUserRequest>
{
    public UpdateUserRequestValidator(IUserRepository repository)
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("user_id_invalid");

        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("user_fullname_required")
            .MaximumLength(200).WithMessage("user_fullname_too_long");

        RuleFor(x => x.RoleName)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("user_role_required")
            .MustAsync(async (role, ct) => await repository.RoleExistsAsync(role, ct))
                .WithMessage("user_role_invalid");

        // A planta padrão deve estar entre as plantas já associadas ao usuário.
        RuleFor(x => x.DefaultSiteId)
            .Cascade(CascadeMode.Stop)
            .GreaterThan(0).WithMessage("user_default_site_required")
            .MustAsync(async (request, id, ct) => await repository.IsSiteLinkedAsync(request.Id, id, ct))
                .WithMessage("user_default_site_not_linked");
    }
}
