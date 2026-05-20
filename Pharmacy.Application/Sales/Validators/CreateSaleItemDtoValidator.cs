using FluentValidation;
using Pharmacy.Application.Sales.DTOs;

namespace Pharmacy.Application.Sales.Validators;

public sealed class CreateSaleItemDtoValidator : AbstractValidator<CreateSaleItemDto>
{
    public CreateSaleItemDtoValidator()
    {
        RuleFor(x => x.MedicineId)
            .GreaterThan(0);

        RuleFor(x => x.Quantity)
            .GreaterThan(0);
    }
}
