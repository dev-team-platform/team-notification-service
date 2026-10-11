using Microsoft.AspNetCore.SignalR;
using TeamNotificationService.Application.Interfaces.Services.Notifications;
using TeamNotificationService.Application.Models.Notifications;

namespace TeamNotificationService.Api.Hubs;

public sealed class SignalRRealtimeNotificationPublisher : IRealtimeNotificationPublisher
{
    private readonly IHubContext<NotificationHub, INotificationClient> _hubContext;

    public SignalRRealtimeNotificationPublisher(
        IHubContext<NotificationHub, INotificationClient> hubContext)
    {
        _hubContext = hubContext;
    }

    public Task PublishBellNotificationAsync(
        string identitySubject,
        SendBellNotificationMessage notification,
        CancellationToken cancellationToken = default)
    {
        var sendNotification = notification.Clone();
        sendNotification.RecipientId = Guid.Empty;

        return _hubContext.Clients.Group(identitySubject).BellNotification(sendNotification);
    }
}
