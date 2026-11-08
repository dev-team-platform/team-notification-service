using System.Text.Json;
using AwesomeAssertions;
using Moq;
using TeamNotificationService.Application.Interfaces.Repositories;
using TeamNotificationService.Application.Interfaces.Services.Emails;
using TeamNotificationService.Application.Interfaces.Services.NotificationDeliveries;
using TeamNotificationService.Application.Models.Emails;
using TeamNotificationService.Application.Models.Messaging.Consuming;
using TeamNotificationService.Application.Models.NotificationDeliveries;
using TeamNotificationService.Application.Models.Notifications;
using TeamNotificationService.Domain.Entities;
using TeamNotificationService.Domain.Exceptions;
using TeamNotificationService.Infrastructure.Services.RabbitMq;
using TeamNotificationService.Infrastructure.Services.RabbitMq.Consumers;
using Xunit;

namespace TeamNotificationService.Infrastructure.Tests.Services.RabbitMq.Consumers;

public class NotificationEmailSenderTests
{
    private readonly Mock<IEmailTemplateRepository> _templates = new();
    private readonly Mock<INotificationDeliveryCommandService> _deliveries = new();
    private readonly Mock<IEmailService> _emails = new();

    [Fact]
    public async Task SendAsync_ShouldDoNothing_WhenEmailIsNotRequested()
    {
        var envelope = Envelope(Guid.NewGuid());
        var withoutEmail = new CloudEventEnvelope<UpsertNotificationRequestModel>
        {
            Id = envelope.Id, Source = envelope.Source, Type = envelope.Type,
            Data = new UpsertNotificationRequestModel { SourceApp = "app", EventType = "test", CreatedById = Guid.NewGuid() }
        };

        await Sender().SendAsync(withoutEmail, [], CancellationToken.None);

        _templates.Verify(x => x.FindFirstByConditionAsync(It.IsAny<Func<IQueryable<EmailTemplate>, IQueryable<EmailTemplate>>>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
        _emails.Verify(x => x.QueueEmailAsync(It.IsAny<SendEmailToRecipientRequestModel>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SendAsync_ShouldRejectMissingTemplate_WhenEmailIsRequested()
    {
        var action = () => Sender().SendAsync(Envelope(Guid.NewGuid()), [], CancellationToken.None);

        await action.Should().ThrowAsync<NotFoundException>();
        _emails.Verify(x => x.QueueEmailAsync(It.IsAny<SendEmailToRecipientRequestModel>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SendAsync_ShouldRenderAndQueueWithoutTracking_WhenNoBellRecipientExists()
    {
        SetupTemplate();
        var userId = Guid.NewGuid();

        await Sender().SendAsync(Envelope(userId), [], CancellationToken.None);

        _emails.Verify(x => x.QueueEmailAsync(It.Is<SendEmailToRecipientRequestModel>(message =>
            message.ToEmail == "user@example.test" && message.Subject == "Hello Alice" && message.Body == "Welcome Alice"), It.IsAny<CancellationToken>()), Times.Once);
        _deliveries.Verify(x => x.UpsertDeliveryAsync(It.IsAny<UpsertNotificationDeliveryRequestModel>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SendAsync_ShouldTrackFailure_WhenQueueingFailsForBellRecipient()
    {
        SetupTemplate();
        _emails.Setup(x => x.QueueEmailAsync(It.IsAny<SendEmailToRecipientRequestModel>(), It.IsAny<CancellationToken>())).ThrowsAsync(new InvalidOperationException("queue unavailable"));
        var userId = Guid.NewGuid();
        var recipientId = Guid.NewGuid();
        var bell = new RealtimeBellNotification("app", userId, "subject", recipientId, new SendBellNotificationMessage());

        await Sender().SendAsync(Envelope(userId), [bell], CancellationToken.None);

        _deliveries.Verify(x => x.UpsertDeliveryAsync(It.Is<UpsertNotificationDeliveryRequestModel>(model =>
            model.NotificationRecipientId == recipientId && model.Channel == "Email" && model.Status == "Failed" && model.LastError == "queue unavailable"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendAsync_ShouldPropagateCancellation_WhenQueueIsCanceled()
    {
        SetupTemplate();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        _emails.Setup(x => x.QueueEmailAsync(It.IsAny<SendEmailToRecipientRequestModel>(), cancellation.Token)).ThrowsAsync(new OperationCanceledException(cancellation.Token));

        var action = () => Sender().SendAsync(Envelope(Guid.NewGuid()), [], cancellation.Token);

        await action.Should().ThrowAsync<OperationCanceledException>();
        _deliveries.Verify(x => x.UpsertDeliveryAsync(It.IsAny<UpsertNotificationDeliveryRequestModel>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private void SetupTemplate() => _templates
        .Setup(x => x.FindFirstByConditionAsync(It.IsAny<Func<IQueryable<EmailTemplate>, IQueryable<EmailTemplate>>>(), false, It.IsAny<CancellationToken>()))
        .ReturnsAsync(new EmailTemplate { TemplateKey = "welcome", IsActive = true, SubjectTemplate = "Hello {{name}}", BodyTemplate = "Welcome {{name}}" });

    private NotificationEmailSender Sender() => new(_templates.Object, _deliveries.Object, _emails.Object, Mock.Of<Serilog.ILogger>());

    private static CloudEventEnvelope<UpsertNotificationRequestModel> Envelope(Guid userId) => new()
    {
        Id = Guid.NewGuid(), Source = "test", Type = "test",
        Data = new UpsertNotificationRequestModel
        {
            SourceApp = "app", EventType = "test", CreatedById = Guid.NewGuid(),
            Email = new SendEmailFromProducerRequestModel
            {
                TemplateKey = "welcome", Data = JsonDocument.Parse("{\"name\":\"Alice\"}"),
                Recipients = [new() { UserId = userId, Email = "user@example.test" }]
            }
        }
    };
}
