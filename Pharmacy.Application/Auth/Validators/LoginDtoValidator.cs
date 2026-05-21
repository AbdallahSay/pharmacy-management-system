using FluentValidation;
using Pharmacy.Application.Auth.DTOs;

namespace Pharmacy.Application.Auth.Validators;

public sealed class LoginDtoValidator : AbstractValidator<LoginDto>
{
    public LoginDtoValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(256);

        RuleFor(x => x.Password)
            .NotEmpty();

        RuleFor(x => x.TenantSlug)
            .NotEmpty()
            .MaximumLength(100);
    }
}
