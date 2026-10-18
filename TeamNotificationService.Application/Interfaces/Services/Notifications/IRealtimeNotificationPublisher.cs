using TeamNotificationService.Application.Models.Notifications;

namespace TeamNotificationService.Application.Interfaces.Services.Notifications;

public interface IRealtimeNotificationPublisher
{
    Task PublishBellNotificationAsync(
        string sourceApp,
        string identitySubject,
        SendBellNotificationMessage notification,
        CancellationToken cancellationToken = default);
}
