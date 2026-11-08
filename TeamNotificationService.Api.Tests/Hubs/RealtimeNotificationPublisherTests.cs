using AwesomeAssertions;
using Microsoft.AspNetCore.SignalR;
using Moq;
using TeamNotificationService.Api.Hubs;
using TeamNotificationService.Application.Models.Notifications;
using Xunit;

namespace TeamNotificationService.Api.Tests.Hubs;

public class RealtimeNotificationPublisherTests
{
    [Fact]
    public async Task PublishBellNotificationAsync_ShouldSendSanitizedCopyToUserGroup_WhenCalled()
    {
        var recipient = Guid.NewGuid();
        var message = new SendBellNotificationMessage { NotificationId = Guid.NewGuid(), RecipientId = recipient, Title = "Title", SentAt = DateTimeOffset.UtcNow };
        SendBellNotificationMessage? sent = null;
        var client = new Mock<INotificationClient>();
        client.Setup(x => x.BellNotification(It.IsAny<SendBellNotificationMessage>()))
            .Callback<SendBellNotificationMessage>(value => sent = value).Returns(Task.CompletedTask);
        var clients = new Mock<IHubClients<INotificationClient>>();
        clients.Setup(x => x.Group("app:subject")).Returns(client.Object);
        var hub = new Mock<IHubContext<NotificationHub, INotificationClient>>();
        hub.SetupGet(x => x.Clients).Returns(clients.Object);

        await new SignalRRealtimeNotificationPublisher(hub.Object).PublishBellNotificationAsync("app", "subject", message);

        sent.Should().NotBeNull(); sent.Should().NotBeSameAs(message);
        sent!.RecipientId.Should().BeEmpty(); sent.NotificationId.Should().Be(message.NotificationId);
        sent.SentAt.Should().Be(message.SentAt);
        message.RecipientId.Should().Be(recipient);
        clients.Verify(x => x.Group("app:subject"), Times.Once);
    }
}
