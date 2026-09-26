using TeamNotificationService.Application.Models.Notifications;

namespace TeamNotificationService.Application.Interfaces.Messaging;

public interface IRealtimeNotificationPublisher
{
    Task PublishBellNotificationAsync(
        string identitySubject,
        SendBellNotificationMessage notification,
        CancellationToken cancellationToken = default);
}
