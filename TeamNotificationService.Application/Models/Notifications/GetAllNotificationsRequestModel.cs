using TeamNotificationService.Application.Models.Common;

namespace TeamNotificationService.Application.Models.Notifications;

public class GetAllNotificationsRequestModel : FilterQuery<GetAllNotificationsResponseModelItem>
{
    public override FilterQueryMap<GetAllNotificationsResponseModelItem> GetFilterQueryMap()
    {
        return new FilterQueryMap<GetAllNotificationsResponseModelItem>()
            .Map("notificationId", x => x.NotificationId)
            .Map("title", x => x.Title)
            .Map("content", x => x.Content)
            .Map("createdAt", x => x.CreatedAt)
            .Map("isRead", x => x.IsRead)
            .Map("readAt", x => x.ReadAt);
    }
}