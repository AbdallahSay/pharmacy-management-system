using FluentValidation;
using Pharmacy.Application.Sales.DTOs;
using Pharmacy.Domain.Common;

namespace Pharmacy.Application.Sales.Validators;

public sealed class GetSalesQueryDtoValidator : AbstractValidator<GetSalesQueryDto>
{
    public GetSalesQueryDtoValidator()
    {
        RuleFor(x => x.Skip)
            .GreaterThanOrEqualTo(0);

        RuleFor(x => x.Take)
            .GreaterThan(0)
            .LessThanOrEqualTo(RepositoryLimits.MaxPageSize);
    }
}
