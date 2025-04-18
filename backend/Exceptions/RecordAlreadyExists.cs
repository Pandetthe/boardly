namespace Boardly.Backend.Exceptions;

public class RecordAlreadyExists : Exception
{
    public RecordAlreadyExists() : base()
    {
    }

    public RecordAlreadyExists(string? message) : base(message)
    {
    }

    public RecordAlreadyExists(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}
