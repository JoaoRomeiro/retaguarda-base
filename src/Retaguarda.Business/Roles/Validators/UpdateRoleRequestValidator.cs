using FluentValidation;
using Retaguarda.Business.Roles.Dtos;
using Retaguarda.Data.Repositories;

namespace Retaguarda.Business.Roles.Validators;

public sealed class UpdateRoleRequestValidator : AbstractValidator<UpdateRoleRequest>
{
    public UpdateRoleRequestValidator(IRoleRepository repository)
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("role_id_invalid");

        RuleFor(x => x.Name)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("role_name_required")
            .MaximumLength(256).WithMessage("role_name_too_long")
            // Exclui o próprio registro da checagem de unicidade.
            .MustAsync(async (request, name, ct) => !await repository.NameExistsAsync(name, request.Id, ct))
                .WithMessage("role_name_in_use");

        RuleFor(x => x.Description)
            .MaximumLength(200).WithMessage("role_description_too_long");
    }
}
