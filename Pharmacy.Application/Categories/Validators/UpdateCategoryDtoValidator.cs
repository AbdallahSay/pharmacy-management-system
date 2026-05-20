using FluentValidation;
using Pharmacy.Application.Categories.DTOs;

namespace Pharmacy.Application.Categories.Validators;

public sealed class UpdateCategoryDtoValidator : AbstractValidator<UpdateCategoryDto>
{
    public UpdateCategoryDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(100);
    }
}
