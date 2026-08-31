using FluentValidation;
using Retaguarda.Business.Users.Dtos;
using Retaguarda.Data.Repositories;

namespace Retaguarda.Business.Users.Validators;

public sealed class AssociateSiteRequestValidator : AbstractValidator<AssociateSiteRequest>
{
    public AssociateSiteRequestValidator(IUserRepository repository)
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("user_id_invalid");

        RuleFor(x => x.SiteId)
            .Cascade(CascadeMode.Stop)
            .GreaterThan(0).WithMessage("usersite_site_required")
            // Existe, está ativa e ainda não associada ao usuário.
            .MustAsync(async (request, siteId, ct) =>
                await repository.IsSiteAvailableForUserAsync(request.UserId, siteId, ct))
                .WithMessage("usersite_site_invalid");
    }
}
