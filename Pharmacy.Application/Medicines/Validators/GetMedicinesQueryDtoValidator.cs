using FluentValidation;
using Pharmacy.Application.Medicines.DTOs;
using Pharmacy.Domain.Common;

namespace Pharmacy.Application.Medicines.Validators;

public sealed class GetMedicinesQueryDtoValidator : AbstractValidator<GetMedicinesQueryDto>
{
    public GetMedicinesQueryDtoValidator()
    {
        RuleFor(x => x.Skip)
            .GreaterThanOrEqualTo(0);

        RuleFor(x => x.Take)
            .GreaterThan(0)
            .LessThanOrEqualTo(RepositoryLimits.MaxPageSize);
    }
}
