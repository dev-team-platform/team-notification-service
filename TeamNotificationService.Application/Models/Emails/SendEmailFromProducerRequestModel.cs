using System.Text.Json;

namespace TeamNotificationService.Application.Models.Emails;

public class SendEmailFromProducerRequestModel
{
    public string? FromEmail { get; init; }
    public string? FromName { get; init; }
    public required IReadOnlyList<SendEmailRecipientFromProducerRequestModel> Recipients { get; init; }
    public required string TemplateKey { get; init; }
    public JsonDocument? Data { get; init; }
}

public class SendEmailRecipientFromProducerRequestModel
{
    public required Guid UserId { get; init; }
    public required string Email { get; init; }
}
