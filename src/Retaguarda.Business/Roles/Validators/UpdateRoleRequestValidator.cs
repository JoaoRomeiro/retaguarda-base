using FluentValidation;
using Retaguarda.Business.Roles.Dtos;
using Retaguarda.Data.Repositories;
using Retaguarda.Shared.Authorization;

namespace Retaguarda.Business.Roles.Validators;

public sealed class UpdateRoleRequestValidator : AbstractValidator<UpdateRoleRequest>
{
    public UpdateRoleRequestValidator(IRoleRepository repository, IPermissionCatalog catalog)
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

        // Permissão fora do catálogo é rejeitada: só pode chegar aqui por POST forjado (a tela é
        // uma lista fechada de checkboxes) ou por permissão que saiu do código. Aceitá-la deixaria
        // no banco uma concessão que nunca casa com nada.
        // Entrada em branco é ruído de binding do formulário, não permissão inválida: o serviço a
        // descarta. O que a regra recusa é NOME que não existe no catálogo.
        RuleForEach(x => x.Permissions)
            .Must(permission => string.IsNullOrWhiteSpace(permission) || catalog.Contains(permission))
                .WithMessage("role_permission_unknown");
    }
}
