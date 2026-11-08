using Microsoft.AspNetCore.SignalR;
using TeamNotificationService.Application.Interfaces.Services.Notifications;
using TeamNotificationService.Application.Models.Notifications;
using TeamNotificationService.Domain.Constants;

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
        string sourceApp,
        string identitySubject,
        SendBellNotificationMessage notification,
        CancellationToken cancellationToken = default)
    {
        var sendNotification = notification.Clone();
        sendNotification.RecipientId = Guid.Empty;

        var groupName = NotificationGroupName.ForUser(sourceApp, identitySubject);
        return _hubContext.Clients.Group(groupName).BellNotification(sendNotification);
    }
}
