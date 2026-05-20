using FluentValidation;

namespace Pharmacy.Application.Medicines.Queries.GetMedicineById;

public sealed class GetMedicineByIdQueryValidator : AbstractValidator<GetMedicineByIdQuery>
{
    public GetMedicineByIdQueryValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0);
    }
}
