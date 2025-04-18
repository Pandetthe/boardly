namespace Boardly.Backend.Exceptions;

public class RecordDoesNotExist : Exception
{
    public RecordDoesNotExist() : base()
    {
    }

    public RecordDoesNotExist(string? message) : base(message)
    {
    }

    public RecordDoesNotExist(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}
