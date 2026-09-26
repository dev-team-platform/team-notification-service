using System.Text.Json;
using TeamNotificationService.Application.Models.Notifications;

namespace TeamNotificationService.Api.Dtos.V1.Notifications;

public sealed class GetUnreadNotificationsResponse
{
    public Guid NotificationId { get; init; }
    public string? Title { get; init; }
    public string? Content { get; init; }
    public JsonElement? Data { get; init; }
    public DateTimeOffset CreatedAt { get; init; }

    public static GetUnreadNotificationsResponse FromModel(GetUnreadNotificationsResponseModel model)
    {
        return new GetUnreadNotificationsResponse
        {
            NotificationId = model.NotificationId,
            Title = model.Title,
            Content = model.Content,
            Data = model.Data?.RootElement.Clone(),
            CreatedAt = model.CreatedAt
        };
    }
}
