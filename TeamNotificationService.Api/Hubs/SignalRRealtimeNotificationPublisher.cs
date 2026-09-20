using Microsoft.AspNetCore.SignalR;
using TeamNotificationService.Application.Interfaces.Messaging;
using TeamNotificationService.Application.Models;

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
        BellNotificationMessage notification,
        CancellationToken cancellationToken = default)
    {
        return _hubContext.Clients.Group(identitySubject).BellNotification(notification);
    }
}
