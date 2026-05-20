using FluentValidation;
using Pharmacy.Application.Medicines.DTOs;

namespace Pharmacy.Application.Medicines.Validators;

public sealed class CreateMedicineDtoValidator : AbstractValidator<CreateMedicineDto>
{
    public CreateMedicineDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.Price)
            .GreaterThan(0)
            .LessThanOrEqualTo(999_999.99m);

        RuleFor(x => x.Stock)
            .GreaterThanOrEqualTo(0);

        RuleFor(x => x.CategoryId)
            .GreaterThan(0);
    }
}
