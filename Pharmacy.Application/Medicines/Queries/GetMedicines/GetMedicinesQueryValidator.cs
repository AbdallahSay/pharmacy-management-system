using FluentValidation;
using Pharmacy.Domain.Common;

namespace Pharmacy.Application.Medicines.Queries.GetMedicines;

public sealed class GetMedicinesQueryValidator : AbstractValidator<GetMedicinesQuery>
{
    public GetMedicinesQueryValidator()
    {
        RuleFor(x => x.Skip)
            .GreaterThanOrEqualTo(0);

        RuleFor(x => x.Take)
            .GreaterThan(0)
            .LessThanOrEqualTo(RepositoryLimits.MaxPageSize);
    }
}
