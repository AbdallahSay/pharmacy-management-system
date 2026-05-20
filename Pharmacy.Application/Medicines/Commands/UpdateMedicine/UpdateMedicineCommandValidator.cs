using FluentValidation;

namespace Pharmacy.Application.Medicines.Commands.UpdateMedicine;

public sealed class UpdateMedicineCommandValidator : AbstractValidator<UpdateMedicineCommand>
{
    public UpdateMedicineCommandValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0);

        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.Description)
            .MaximumLength(500)
            .When(x => x.Description is not null);

        RuleFor(x => x.Price)
            .GreaterThan(0)
            .LessThanOrEqualTo(999_999.99m);

        RuleFor(x => x.Stock)
            .GreaterThanOrEqualTo(0);

        RuleFor(x => x.MinStock)
            .GreaterThanOrEqualTo(0);

        RuleFor(x => x.ExpiryDate)
            .GreaterThan(DateTime.UtcNow);

        RuleFor(x => x.CategoryId)
            .GreaterThan(0);
    }
}
