using FluentValidation;
using Retaguarda.Business.Roles.Dtos;
using Retaguarda.Data.Repositories;

namespace Retaguarda.Business.Roles.Validators;

// As mensagens são CHAVES de recurso; a camada web/api as localiza via IStringLocalizer.
public sealed class CreateRoleRequestValidator : AbstractValidator<CreateRoleRequest>
{
    public CreateRoleRequestValidator(IRoleRepository repository)
    {
        RuleFor(x => x.Name)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("role_name_required")
            .MaximumLength(256).WithMessage("role_name_too_long")
            .MustAsync(async (name, ct) => !await repository.NameExistsAsync(name, excludeId: null, ct))
                .WithMessage("role_name_in_use");

        RuleFor(x => x.Description)
            .MaximumLength(200).WithMessage("role_description_too_long");
    }
}
