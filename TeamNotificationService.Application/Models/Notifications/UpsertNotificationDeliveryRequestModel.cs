namespace TeamNotificationService.Application.Models.Notifications;

public sealed class UpsertNotificationDeliveryRequestModel
{
    public required Guid NotificationRecipientId { get; init; }
    public required string Channel { get; init; }
    public required string Status { get; init; }
    public string? Destination { get; init; }
    public string? LastError { get; init; }
}
