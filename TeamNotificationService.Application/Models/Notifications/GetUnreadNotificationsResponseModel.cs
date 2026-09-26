using System.Text.Json;

namespace TeamNotificationService.Application.Models.Notifications;

public sealed class GetUnreadNotificationsResponseModel
{
    public Guid NotificationId { get; init; }
    public string? Title { get; init; }
    public string? Content { get; init; }
    public JsonDocument? Data { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
}
