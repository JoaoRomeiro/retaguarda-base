using FluentValidation;
using Retaguarda.Business.Sites.Dtos;
using Retaguarda.Data.Repositories;

namespace Retaguarda.Business.Sites.Validators;

// As mensagens são CHAVES de recurso; a camada web/api as localiza via IStringLocalizer.
public sealed class CreateSiteRequestValidator : AbstractValidator<CreateSiteRequest>
{
    public CreateSiteRequestValidator(ISiteRepository repository)
    {
        RuleFor(x => x.Name)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("site_name_required")
            .MaximumLength(200).WithMessage("site_name_too_long")
            .MustAsync(async (name, ct) => !await repository.NameExistsAsync(name, excludeId: null, ct))
                .WithMessage("site_name_in_use");

        RuleFor(x => x.Code)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("site_code_required")
            .MaximumLength(20).WithMessage("site_code_too_long")
            .MustAsync(async (code, ct) => !await repository.CodeExistsAsync(code, excludeId: null, ct))
                .WithMessage("site_code_in_use");

        RuleFor(x => x.TimeZone)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("site_timezone_required")
            .Must(BrazilTimeZones.Contains).WithMessage("site_timezone_invalid");
    }
}
