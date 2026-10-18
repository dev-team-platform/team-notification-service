using System.Text.Json;

namespace TeamNotificationService.Application.Models.Notifications;

public sealed class GetLatestNotificationForUserResponseModel
{
    public Guid NotificationId { get; init; }
    public string? Title { get; init; }
    public string? Content { get; init; }
    public JsonDocument? Data { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public bool IsRead { get; init; }
    public DateTimeOffset? ReadAt { get; init; }
}
