using FluentValidation;
using Retaguarda.Business.Sites.Dtos;
using Retaguarda.Data.Repositories;

namespace Retaguarda.Business.Sites.Validators;

public sealed class UpdateSiteRequestValidator : AbstractValidator<UpdateSiteRequest>
{
    public UpdateSiteRequestValidator(ISiteRepository repository)
    {
        RuleFor(x => x.Id)
            .GreaterThan(0).WithMessage("site_id_invalid");

        RuleFor(x => x.Name)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("site_name_required")
            .MaximumLength(200).WithMessage("site_name_too_long")
            .MustAsync(async (request, name, ct) => !await repository.NameExistsAsync(name, request.Id, ct))
                .WithMessage("site_name_in_use");

        RuleFor(x => x.Code)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("site_code_required")
            .MaximumLength(20).WithMessage("site_code_too_long")
            // Exclui o próprio registro da checagem de unicidade.
            .MustAsync(async (request, code, ct) => !await repository.CodeExistsAsync(code, request.Id, ct))
                .WithMessage("site_code_in_use");

        RuleFor(x => x.TimeZone)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("site_timezone_required")
            .Must(BrazilTimeZones.Contains).WithMessage("site_timezone_invalid");
    }
}
