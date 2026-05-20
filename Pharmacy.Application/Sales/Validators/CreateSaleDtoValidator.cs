using FluentValidation;
using Pharmacy.Application.Sales.DTOs;

namespace Pharmacy.Application.Sales.Validators;

public sealed class CreateSaleDtoValidator : AbstractValidator<CreateSaleDto>
{
    public CreateSaleDtoValidator()
    {
        RuleFor(x => x.Items)
            .NotEmpty()
            .WithMessage("At least one sale item is required.");

        RuleForEach(x => x.Items)
            .SetValidator(new CreateSaleItemDtoValidator());

        RuleFor(x => x.Items)
            .Must(items => items.Select(i => i.MedicineId).Distinct().Count() == items.Count)
            .WithMessage("Duplicate medicine lines are not allowed in a single sale.");
    }
}
