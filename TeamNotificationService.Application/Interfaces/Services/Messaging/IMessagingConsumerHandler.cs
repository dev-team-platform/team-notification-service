namespace TeamNotificationService.Application.Interfaces.Services.Messaging;

public interface IMessagingConsumerHandler
{
    Task HandleAsync(ReadOnlyMemory<byte> body, CancellationToken cancellationToken);
}