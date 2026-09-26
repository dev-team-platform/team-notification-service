namespace TeamNotificationService.Application.Models.Notifications;

public sealed class MarkNotificationReadRequestModel
{
    public Guid? NotificationId { get; init; }
    public bool IsAll { get; init; }
}
