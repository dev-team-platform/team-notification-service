using System.Text.Json;
using TeamNotificationService.Api.Dtos.Common;
using TeamNotificationService.Application.Models.Notifications;

namespace TeamNotificationService.Api.Dtos.V1.Notifications;

public class GetAllNotificationsRequest : FilterRequest<GetAllNotificationsResponseItem>
{
    public override FilterQueryMapRequest<GetAllNotificationsResponseItem> GetFilterRequestMap()
    {
        return new FilterQueryMapRequest<GetAllNotificationsResponseItem>()
            .Map("notificationId", x => x.NotificationId)
            .Map("title", x => x.Title)
            .Map("content", x => x.Content)
            .Map("createdAt", x => x.CreatedAt)
            .Map("isRead", x => x.IsRead)
            .Map("readAt", x => x.ReadAt);
    }

    public static GetAllNotificationsRequestModel ToModel(GetAllNotificationsRequest request)
    {
        return new GetAllNotificationsRequestModel
        {
            CurrentPage = request.CurrentPage,
            ItemsPerPage = request.ItemsPerPage,
            SearchGlobalText = request.SearchGlobalText,
            FilterGroup = request.BuildFilterGroup(),
            FilterCriteria = request.BuildFilterCriteria(),
            SortFields = request.BuildSortFields()
        };
    }
}
