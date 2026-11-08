using System.Text.Json;

namespace TeamNotificationService.Application.Models.Messaging.Consuming;

public class UpsertNotificationRequestModel
{
    public required string SourceApp { get; init; }
    public required string EventType { get; init; }
    public required Guid CreatedById { get; init; }
    public Guid? UpdatedById { get; init; }
    public NotificationRequestModel? Notification { get; init; }
    public SendEmailFromProducerRequestModel? Email { get; init; }

}

public class NotificationRequestModel
{
    public Guid? NotificationId { get; init; }
    public required string Title { get; init; }
    public required string Content { get; init; }
    public required IReadOnlyList<NotificationRecipientRequestModel> Recipients { get; init; }
    public JsonDocument? Data { get; init; }
}

public class NotificationRecipientRequestModel
{
    public required Guid UserId { get; init; }
    public required string IdentitySubject { get; init; }
}
