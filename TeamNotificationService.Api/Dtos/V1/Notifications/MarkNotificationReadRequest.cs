using TeamNotificationService.Application.Models.Notifications;

namespace TeamNotificationService.Api.Dtos.V1.Notifications;

public sealed class MarkNotificationReadRequest
{
    public Guid? NotificationId { get; init; }
    public bool IsAll { get; init; }

    public static MarkNotificationReadRequestModel ToModel(MarkNotificationReadRequest request)
    {
        return new MarkNotificationReadRequestModel
        {
            NotificationId = request.NotificationId,
            IsAll = request.IsAll
        };
    }
}
