using FluentValidation;

namespace Pharmacy.Application.Auth.Commands.RevokeAllTokens;

public sealed class RevokeAllTokensCommandValidator : AbstractValidator<RevokeAllTokensCommand>
{
    public RevokeAllTokensCommandValidator()
    {
        RuleFor(x => x.UserId)
            .GreaterThan(0)
            .WithMessage("Valid User ID is required.");
    }
}
