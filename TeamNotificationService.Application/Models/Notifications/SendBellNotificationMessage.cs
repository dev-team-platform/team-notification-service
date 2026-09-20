using System.Text.Json;

namespace TeamNotificationService.Application.Models.Notifications;

public sealed class BellNotificationMessage
{
    public required Guid NotificationId { get; init; }
    public required Guid RecipientId { get; init; }
    public required string EventType { get; init; }
    public required string Message { get; init; }
    public JsonDocument? Data { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
}
