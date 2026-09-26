using TeamNotificationService.Application.Models.Notifications;

namespace TeamNotificationService.Api.Dtos.V1.Notifications;

public sealed class MarkNotificationReadResponse
{
    public int MarkedCount { get; init; }

    public static MarkNotificationReadResponse FromModel(MarkNotificationReadResponseModel model)
    {
        return new MarkNotificationReadResponse
        {
            MarkedCount = model.MarkedCount
        };
    }
}
