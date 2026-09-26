using TeamNotificationService.Application.Models.Notifications;

namespace TeamNotificationService.Api.Hubs;

public interface INotificationClient
{
    Task BellNotification(SendBellNotificationMessage notification);
}
