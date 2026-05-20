using FluentValidation;
using Pharmacy.Application.Categories.DTOs;

namespace Pharmacy.Application.Categories.Validators;

public sealed class CreateCategoryDtoValidator : AbstractValidator<CreateCategoryDto>
{
    public CreateCategoryDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(100);
    }
}
