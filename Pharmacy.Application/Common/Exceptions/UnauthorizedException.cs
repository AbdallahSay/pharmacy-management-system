namespace Pharmacy.Application.Common.Exceptions;

public class UnauthorizedException : Exception
{
    public UnauthorizedException(string message = "Invalid credentials.")
        : base(message)
    {
    }
}
