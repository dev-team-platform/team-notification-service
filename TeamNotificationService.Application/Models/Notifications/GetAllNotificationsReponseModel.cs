using System.Text.Json;
using TeamNotificationService.Application.Models.Common;

namespace TeamNotificationService.Application.Models.Notifications;

public class GetAllNotificationsReponseModel : FilterResult<GetAllNotificationsResponseModelItem>
{
    public static GetAllNotificationsReponseModel FromFilterResult(FilterResult<GetAllNotificationsResponseModelItem> filterResult)
    {
        return new GetAllNotificationsReponseModel
        {
            Items = filterResult.Items,
            TotalItems = filterResult.TotalItems,
            TotalPages = filterResult.TotalPages,
            ItemsPerPage = filterResult.ItemsPerPage,
            CurrentPage = filterResult.CurrentPage
        };
    }
}

public class GetAllNotificationsResponseModelItem
{
    public Guid NotificationId { get; init; }
    public string? Title { get; init; }
    public string? Content { get; init; }
    public JsonDocument? Data { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public bool IsRead { get; init; }
    public DateTimeOffset? ReadAt { get; init; }
}