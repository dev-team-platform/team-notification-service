namespace TeamNotificationService.Domain.Exceptions;

public class UnprocessableEntityException : Exception
{
    public Dictionary<string, object?>? Details { get; set; }

    public UnprocessableEntityException(string message, Dictionary<string, object?>? details = null, Exception? innerException = null) :
        base(message, innerException)
    {
        Details = details;
    }
}