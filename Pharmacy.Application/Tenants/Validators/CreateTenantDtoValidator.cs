using FluentValidation;
using Pharmacy.Application.Tenants.DTOs;

namespace Pharmacy.Application.Tenants.Validators;

public sealed class CreateTenantDtoValidator : AbstractValidator<CreateTenantDto>
{
    private static readonly System.Text.RegularExpressions.Regex SlugRegex =
        new(@"^[a-z0-9]+(?:-[a-z0-9]+)*$",
            System.Text.RegularExpressions.RegexOptions.Compiled);

    public CreateTenantDtoValidator()
    {
        RuleFor(x => x.PharmacyName)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.Slug)
            .NotEmpty()
            .MaximumLength(100)
            .Must(slug => SlugRegex.IsMatch(slug))
            .WithMessage("Slug must be lowercase letters, digits, and hyphens only (e.g. 'nile-pharmacy').");

        RuleFor(x => x.AdminFullName)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.AdminEmail)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(256);

        RuleFor(x => x.AdminPassword)
            .NotEmpty()
            .MinimumLength(8)
            .Matches(@"[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
            .Matches(@"[a-z]").WithMessage("Password must contain at least one lowercase letter.")
            .Matches(@"\d").WithMessage("Password must contain at least one digit.");
    }
}