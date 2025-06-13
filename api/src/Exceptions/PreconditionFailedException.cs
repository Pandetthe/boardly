namespace Boardly.Api.Exceptions;

public class PreconditionFailedException : Exception
{
    public PreconditionFailedException() : base()
    {
    }

    public PreconditionFailedException(string? message) : base(message)
    {
    }

    public PreconditionFailedException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}