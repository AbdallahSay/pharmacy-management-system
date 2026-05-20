using FluentValidation;
using Pharmacy.Application.Categories.DTOs;
using Pharmacy.Domain.Common;

namespace Pharmacy.Application.Categories.Validators;

public sealed class GetCategoriesQueryDtoValidator : AbstractValidator<GetCategoriesQueryDto>
{
    public GetCategoriesQueryDtoValidator()
    {
        RuleFor(x => x.Skip)
            .GreaterThanOrEqualTo(0);

        RuleFor(x => x.Take)
            .GreaterThan(0)
            .LessThanOrEqualTo(RepositoryLimits.MaxPageSize);
    }
}
