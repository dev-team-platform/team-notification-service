using TeamNotificationService.Application.Models.Notifications;

namespace TeamNotificationService.Api.Dtos.V1.Notifications;

public sealed class MarkNotificationReadRequest
{
    public IReadOnlyList<Guid>? NotificationIds { get; init; }
    public bool IsAll { get; init; }

    public static MarkNotificationReadRequestModel ToModel(MarkNotificationReadRequest request)
    {
        return new MarkNotificationReadRequestModel
        {
            NotificationIds = (request.NotificationIds ?? Array.Empty<Guid>())
                .Distinct()
                .ToArray(),
            IsAll = request.IsAll
        };
    }
}
