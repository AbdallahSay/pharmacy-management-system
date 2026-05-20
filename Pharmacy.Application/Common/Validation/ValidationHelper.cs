using FluentValidation;
using FluentValidation.Results;

namespace Pharmacy.Application.Common.Validation;

internal static class ValidationHelper
{
    public static async Task ValidateAndThrowAsync<T>(
        T instance,
        IValidator<T> validator,
        CancellationToken cancellationToken)
    {
        var result = await validator.ValidateAsync(instance, cancellationToken);

        if (!result.IsValid)
            throw new ValidationException(result.Errors);
    }

    public static void ValidateId(int id, string parameterName = "id")
    {
        if (id <= 0)
        {
            throw new ValidationException(new[]
            {
                new ValidationFailure(parameterName, $"{parameterName} must be greater than zero.")
            });
        }
    }
}
