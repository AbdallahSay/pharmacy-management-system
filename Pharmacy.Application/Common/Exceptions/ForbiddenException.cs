namespace Pharmacy.Application.Common.Exceptions;

public class ForbiddenException : Exception
{
    public ForbiddenException(string message = "You do not have access to this tenant.")
        : base(message)
    {
    }
}
