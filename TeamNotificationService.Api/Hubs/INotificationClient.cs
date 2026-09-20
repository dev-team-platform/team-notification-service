using TeamNotificationService.Application.Models;

namespace TeamNotificationService.Api.Hubs;

public interface INotificationClient
{
    Task BellNotification(BellNotificationMessage notification);
}
