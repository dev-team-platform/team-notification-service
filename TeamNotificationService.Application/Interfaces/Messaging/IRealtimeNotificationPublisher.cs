using TeamNotificationService.Application.Models;
using TeamNotificationService.Application.Models.Notification;

namespace TeamNotificationService.Application.Interfaces.Messaging;

public interface IRealtimeNotificationPublisher
{
    Task PublishBellNotificationAsync(
        string identitySubject,
        BellNotificationMessage notification,
        CancellationToken cancellationToken = default);
}
