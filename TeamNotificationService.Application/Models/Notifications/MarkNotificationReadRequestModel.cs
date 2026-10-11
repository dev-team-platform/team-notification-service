namespace TeamNotificationService.Application.Models.Notifications;

public sealed class MarkNotificationReadRequestModel
{
    public IReadOnlyList<Guid> NotificationIds { get; init; } = [];
    public bool IsAll { get; init; }
}
