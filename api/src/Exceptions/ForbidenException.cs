namespace Boardly.Api.Exceptions;

public class ForbidenException : Exception
{
    public ForbidenException() : base()
    {
    }

    public ForbidenException(string? message) : base(message)
    {
    }

    public ForbidenException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}