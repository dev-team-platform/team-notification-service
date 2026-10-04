namespace TeamNotificationService.Domain.Exceptions;

public sealed class InvalidEmailTemplateException : Exception
{
    public Dictionary<string, object?>? Details { get; set; }

    public InvalidEmailTemplateException(string message, Dictionary<string, object?>? details = null, Exception? innerException = null) :
        base(message, innerException)
    {
        Details = details;
    }
}