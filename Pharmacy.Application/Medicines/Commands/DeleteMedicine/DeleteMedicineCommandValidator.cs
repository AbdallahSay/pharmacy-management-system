using FluentValidation;

namespace Pharmacy.Application.Medicines.Commands.DeleteMedicine;

public sealed class DeleteMedicineCommandValidator : AbstractValidator<DeleteMedicineCommand>
{
    public DeleteMedicineCommandValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0);
    }
}
