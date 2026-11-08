namespace TeamNotificationService.Domain.Exceptions;

public sealed class NonRetryableMessageException : Exception
{
    public Dictionary<string, object?>? Details { get; set; }

    public NonRetryableMessageException(string message, Dictionary<string, object?>? details = null, Exception? innerException = null) :
        base(message, innerException)
    {
        Details = details;
    }
}