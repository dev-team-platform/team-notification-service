namespace TeamNotificationService.Application.Models.Emails;

public class SendEmailToRecipientRequestModel
{
    public string? FromEmail { get; init; }
    public string? FromName { get; init; }
    public string ToEmail { get; init; } = null!;
    public string Subject { get; init; } = null!;
    public string Body { get; init; } = null!;
    public bool IsBodyHtml { get; init; } = true;
}
