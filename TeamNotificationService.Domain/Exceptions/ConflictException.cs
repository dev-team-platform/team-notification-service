namespace TeamNotificationService.Domain.Exceptions;

public class ConflictException : Exception
{
    public Dictionary<string, object>? Details { get; set; }

    public ConflictException(string message, Dictionary<string, object>? details = null, Exception? innerException = null) :
        base(message, innerException)
    {
        Details = details;
    }
}