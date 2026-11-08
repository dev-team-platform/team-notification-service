namespace TeamNotificationService.Application.Interfaces.Services.Messaging;

public interface IMessagingPublisherHandler<in TMessage>
{
    Task HandleAsync(TMessage message, CancellationToken cancellationToken = default);
}