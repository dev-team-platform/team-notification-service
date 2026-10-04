using System.Text.Json;

namespace TeamNotificationService.Application.Models.Messaging.Consuming;

public class CreateNotificationRequestModel
{
    public required string SourceApp { get; init; }
    public required string EventType { get; init; }
    public required Guid CreatedById { get; init; }
    public NotificationRequestModel? Notification { get; init; }
    public SendEmailFromProducerRequestModel? Email { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
}

public class NotificationRequestModel
{
    public required string Title { get; init; }
    public required string Message { get; init; }
    public required IReadOnlyList<NotificationRecipientRequestModel> Recipients { get; init; }
    public JsonDocument? Data { get; init; }
}

public class NotificationRecipientRequestModel
{
    public required Guid UserId { get; init; }
    public required string IdentitySubject { get; init; }
}