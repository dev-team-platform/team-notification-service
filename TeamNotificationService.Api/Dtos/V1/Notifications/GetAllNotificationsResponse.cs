using System.Text.Json;
using TeamNotificationService.Api.Dtos.Common;
using TeamNotificationService.Application.Models.Notifications;

namespace TeamNotificationService.Api.Dtos.V1.Notifications;

public class GetAllNotificationsResponse : FilterResponse<GetAllNotificationsResponseItem>
{
    public static GetAllNotificationsResponse FromModel(GetAllNotificationsReponseModel model)
    {
        return new GetAllNotificationsResponse
        {
            Items = [.. model.Items.Select(GetAllNotificationsResponseItem.FromModel)],
            CurrentPage = model.CurrentPage,
            ItemsPerPage = model.ItemsPerPage,
            TotalItems = model.TotalItems,
            TotalPages = model.TotalPages
        };
    }
}

public class GetAllNotificationsResponseItem
{
    public Guid NotificationId { get; init; }
    public string? Title { get; init; }
    public string? Content { get; init; }
    public JsonElement? Data { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public bool IsRead { get; init; }
    public DateTimeOffset? ReadAt { get; init; }

    public static GetAllNotificationsResponseItem FromModel(GetAllNotificationsResponseModelItem modelItem)
    {
        return new GetAllNotificationsResponseItem
        {
            NotificationId = modelItem.NotificationId,
            Title = modelItem.Title,
            Content = modelItem.Content,
            Data = modelItem.Data?.RootElement.Clone(),
            CreatedAt = modelItem.CreatedAt,
            IsRead = modelItem.IsRead,
            ReadAt = modelItem.ReadAt
        };
    }
}

