using System.Text;
using System.Text.Json;
using AwesomeAssertions;
using Microsoft.Extensions.Options;
using Moq;
using TeamNotificationService.Application.Interfaces.Repositories;
using TeamNotificationService.Application.Interfaces.Services.Emails;
using TeamNotificationService.Application.Interfaces.Services.NotificationDeliveries;
using TeamNotificationService.Application.Interfaces.Services.Notifications;
using TeamNotificationService.Application.Models.NotificationDeliveries;
using TeamNotificationService.Application.Models.Messaging.Consuming;
using TeamNotificationService.Domain.Entities;
using TeamNotificationService.Infrastructure.Services.RabbitMq;
using TeamNotificationService.Application.Models.Notifications;
using TeamNotificationService.Domain.Exceptions;
using TeamNotificationService.Infrastructure.Options;
using TeamNotificationService.Infrastructure.Services.RabbitMq.Consumers;
using TeamNotificationService.Infrastructure.Persistence.Extensions;
using TeamNotificationService.Application.Models.Common;
using Xunit;

namespace TeamNotificationService.Infrastructure.Tests.Services.RabbitMq.Consumers;

public class NotificationRequestedConsumerTests
{
    [Fact]
    public async Task PersistAsync_ShouldIgnoreDelayedInProgress_WhenExportAlreadySucceeded()
    {
        var id = Guid.NewGuid();
        var existing = new Notification { Id = id, SourceApp = "app", Title = "finished", Data = JsonDocument.Parse("{\"status\":\"Success\"}") };
        var notifications = new Mock<INotificationRepository>();
        notifications.Setup(x => x.FindFirstByConditionAsync(It.IsAny<Func<IQueryable<Notification>, IQueryable<Notification>>>(), true, It.IsAny<CancellationToken>())).ReturnsAsync(existing);
        var unit = new Mock<IUnitOfWork>();
        var envelope = new CloudEventEnvelope<UpsertNotificationRequestModel>
        {
            Id = Guid.NewGuid(), Source = "gateway", Type = "user.export.notification.v1",
            Data = new UpsertNotificationRequestModel
            {
                SourceApp = "app", EventType = "user.export.notification.v1", CreatedById = Guid.NewGuid(),
                Notification = new NotificationRequestModel
                {
                    NotificationId = id, Title = "pending", Content = "pending", Data = JsonDocument.Parse("{\"status\":\"InProgress\"}"),
                    Recipients = [new() { UserId = Guid.NewGuid(), IdentitySubject = "subject" }]
                }
            }
        };

        var result = await new NotificationPersistence(notifications.Object, Mock.Of<INotificationRecipientRepository>(), unit.Object, Mock.Of<Serilog.ILogger>()).PersistAsync(envelope, CancellationToken.None);

        result.Should().BeEmpty();
        existing.Title.Should().Be("finished");
        unit.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
    [Theory]
    [InlineData("not-json")]
    [InlineData("{}")]
    [InlineData("null")]
    public async Task HandleAsync_ShouldRejectMalformedEvent_WhenPayloadIsInvalid(string body)
    {
        var service = CreateConsumer();
        var action = () => service.HandleAsync(Encoding.UTF8.GetBytes(body), CancellationToken.None);
        await action.Should().ThrowAsync<NonRetryableMessageException>();
    }

    [Fact]
    public async Task HandleAsync_ShouldPersistAndPublishNotification_WhenEventIsValid()
    {
        var notifications = new Mock<INotificationRepository>();
        var recipients = new Mock<INotificationRecipientRepository>();
        var unit = new Mock<IUnitOfWork>();
        var logger = Mock.Of<Serilog.ILogger>();
        var publisher = new Mock<IRealtimeNotificationPublisher>();
        var deliveries = new Mock<INotificationDeliveryCommandService>();
        unit.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        publisher.Setup(x => x.PublishBellNotificationAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<SendBellNotificationMessage>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        deliveries.Setup(x => x.UpsertDeliveryAsync(It.IsAny<UpsertNotificationDeliveryRequestModel>(), It.IsAny<CancellationToken>())).ReturnsAsync(new UpsertNotifcationDeliveryResponseModel());
        var consumer = new NotificationRequestedConsumer(
            new NotificationPersistence(notifications.Object, recipients.Object, unit.Object, logger),
            new NotificationBellSender(publisher.Object, deliveries.Object, logger),
            new NotificationEmailSender(Mock.Of<IEmailTemplateRepository>(), deliveries.Object, Mock.Of<IEmailService>(), logger),
            Microsoft.Extensions.Options.Options.Create(new SourceAppsOptions { Allowed = ["app"] }));
        var envelope = new CloudEventEnvelope<UpsertNotificationRequestModel>
        {
            Id = Guid.NewGuid(),
            Source = "gateway",
            Type = "notification.created",
            Data = new UpsertNotificationRequestModel
            {
                SourceApp = "app",
                EventType = "test",
                CreatedById = Guid.NewGuid(),
                Notification = new NotificationRequestModel
                {
                    Title = "Hello",
                    Content = "Message",
                    Recipients = [new NotificationRecipientRequestModel { UserId = Guid.NewGuid(), IdentitySubject = "subject" }]
                }
            }
        };

        await consumer.HandleAsync(JsonSerializer.SerializeToUtf8Bytes(envelope), CancellationToken.None);

        notifications.Verify(x => x.Add(It.Is<Notification>(n => n.SourceApp == "app" && n.Title == "Hello")), Times.Once);
        unit.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        publisher.Verify(x => x.PublishBellNotificationAsync("app", "subject", It.Is<SendBellNotificationMessage>(message => message.Title == "Hello"), It.IsAny<CancellationToken>()), Times.Once);
        deliveries.Verify(x => x.UpsertDeliveryAsync(It.Is<UpsertNotificationDeliveryRequestModel>(delivery => delivery.Status == "Delivered"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(false, "Delivered")]
    [InlineData(true, "Failed")]
    public async Task SendAsync_ShouldTrackDelivery_WhenPublishSucceedsOrFails(bool fails, string status)
    {
        var publisher = new Mock<IRealtimeNotificationPublisher>();
        if (fails)
            publisher.Setup(x => x.PublishBellNotificationAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<SendBellNotificationMessage>(), It.IsAny<CancellationToken>())).ThrowsAsync(new InvalidOperationException("offline"));
        else
            publisher.Setup(x => x.PublishBellNotificationAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<SendBellNotificationMessage>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var delivery = new Mock<INotificationDeliveryCommandService>();
        UpsertNotificationDeliveryRequestModel? tracked = null;
        delivery.Setup(x => x.UpsertDeliveryAsync(It.IsAny<UpsertNotificationDeliveryRequestModel>(), It.IsAny<CancellationToken>()))
            .Callback<UpsertNotificationDeliveryRequestModel, CancellationToken>((model, _) => tracked = model)
            .ReturnsAsync(new UpsertNotifcationDeliveryResponseModel());
        var id = Guid.NewGuid();
        var notification = new RealtimeBellNotification("app", Guid.NewGuid(), "subject", id, new SendBellNotificationMessage { NotificationId = Guid.NewGuid() });

        await new NotificationBellSender(publisher.Object, delivery.Object, Mock.Of<Serilog.ILogger>()).SendAsync([notification], CancellationToken.None);

        tracked.Should().NotBeNull(); tracked!.Status.Should().Be(status); tracked.Channel.Should().Be("Bell");
        tracked.NotificationRecipientId.Should().Be(id);
        tracked.LastError.Should().Be(fails ? "offline" : null);
    }

    [Fact]
    public async Task PersistAsync_ShouldCreateNotificationAndRecipients_WhenEventHasBellPayload()
    {
        var notifications = new Mock<INotificationRepository>();
        var recipients = new Mock<INotificationRecipientRepository>();
        var unit = new Mock<IUnitOfWork>();
        Notification? saved = null;
        IReadOnlyList<NotificationRecipient>? savedRecipients = null;
        notifications.Setup(x => x.Add(It.IsAny<Notification>())).Callback<Notification>(value => saved = value).Returns<Notification>(value => value);
        recipients.Setup(x => x.AddRange(It.IsAny<IReadOnlyList<NotificationRecipient>>())).Callback<IReadOnlyList<NotificationRecipient>>(value => savedRecipients = value).Returns<IReadOnlyList<NotificationRecipient>>(value => value);
        unit.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        var user = Guid.NewGuid();
        var envelope = new CloudEventEnvelope<UpsertNotificationRequestModel>
        {
            Id = Guid.NewGuid(),
            Source = "gateway",
            Type = "test",
            Data = new UpsertNotificationRequestModel
            {
                SourceApp = "app",
                EventType = "test",
                CreatedById = Guid.NewGuid(),
                Notification = new NotificationRequestModel
                {
                    Title = "title",
                    Content = "body",
                    Recipients = [new NotificationRecipientRequestModel { UserId = user, IdentitySubject = "subject" }]
                }
            }
        };

        var result = await new NotificationPersistence(notifications.Object, recipients.Object, unit.Object, Mock.Of<Serilog.ILogger>()).PersistAsync(envelope, CancellationToken.None);

        saved.Should().NotBeNull(); saved!.Id.Version.Should().Be(7); saved.SourceApp.Should().Be("app");
        saved.UpdatedAt.Should().BeNull();
        saved.UpdatedById.Should().BeNull();
        savedRecipients.Should().ContainSingle().Which.NotificationId.Should().Be(saved.Id);
        result.Should().ContainSingle().Which.NotificationRecipientId.Should().Be(savedRecipients![0].Id);
        result[0].IdentitySubject.Should().Be("subject");
        unit.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PersistAsync_ShouldUpdateAndReuseRecipients_WhenNotificationExists()
    {
        var notifications = new Mock<INotificationRepository>();
        var recipients = new Mock<INotificationRecipientRepository>();
        var unit = new Mock<IUnitOfWork>();
        var userId = Guid.NewGuid();
        var newUserId = Guid.NewGuid();
        var createdAt = DateTimeOffset.UtcNow.AddDays(-1);
        var readAt = DateTimeOffset.UtcNow.AddHours(-1);
        var existing = new Notification { Id = Guid.NewGuid(), SourceApp = "app", Title = "old", CreatedAt = createdAt, CreatedById = Guid.NewGuid() };
        var existingRecipient = new NotificationRecipient { Id = Guid.NewGuid(), NotificationId = existing.Id, UserId = userId, UserIdentitySubject = "old-subject", IsRead = true, ReadAt = readAt, CreatedAt = createdAt };
        notifications.Setup(x => x.FindFirstByConditionAsync(It.IsAny<Func<IQueryable<Notification>, IQueryable<Notification>>>(), true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);
        recipients.Setup(x => x.FindAllByConditionAsync(It.IsAny<Func<IQueryable<NotificationRecipient>, IQueryable<NotificationRecipient>>>(), true, It.IsAny<CancellationToken>()))
            .ReturnsAsync([existingRecipient]);
        IReadOnlyList<NotificationRecipient>? added = null;
        recipients.Setup(x => x.AddRange(It.IsAny<IReadOnlyList<NotificationRecipient>>()))
            .Callback<IReadOnlyList<NotificationRecipient>>(value => added = value)
            .Returns<IReadOnlyList<NotificationRecipient>>(value => value);
        var envelope = CreateEnvelope(new NotificationRequestModel
        {
            NotificationId = existing.Id,
            Title = "new",
            Content = "updated",
            Data = JsonDocument.Parse("{\"value\":2}"),
            Recipients = [new() { UserId = userId, IdentitySubject = "subject" }, new() { UserId = newUserId, IdentitySubject = "new-subject" }]
        });

        var result = await new NotificationPersistence(notifications.Object, recipients.Object, unit.Object, Mock.Of<Serilog.ILogger>()).PersistAsync(envelope, CancellationToken.None);

        existing.Title.Should().Be("new");
        existing.Content.Should().Be("updated");
        existing.UpdatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
        existing.UpdatedById.Should().Be(envelope.Data.UpdatedById);
        existing.Data!.RootElement.GetProperty("value").GetInt32().Should().Be(2);
        existing.CreatedAt.Should().Be(createdAt);
        existingRecipient.UserIdentitySubject.Should().Be("subject");
        existingRecipient.IsRead.Should().BeFalse();
        existingRecipient.ReadAt.Should().BeNull();
        existingRecipient.UpdatedAt.Should().Be(existing.UpdatedAt);
        result.Should().HaveCount(2);
        result[0].NotificationRecipientId.Should().Be(existingRecipient.Id);
        result[0].Notification.NotificationId.Should().Be(existing.Id);
        result[0].Notification.IsRead.Should().BeFalse();
        result[0].Notification.ReadAt.Should().BeNull();
        result[0].Notification.CreatedAt.Should().Be(createdAt);
        result[0].Notification.UpdatedAt.Should().Be(existingRecipient.UpdatedAt);
        added.Should().ContainSingle().Which.UserId.Should().Be(newUserId);
        added![0].UpdatedAt.Should().BeNull();
        result[1].NotificationRecipientId.Should().Be(added![0].Id);
        notifications.Verify(x => x.Add(It.IsAny<Notification>()), Times.Never);
        unit.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PersistAsync_ShouldCreateWithRequestedId_WhenNotificationDoesNotExist()
    {
        var notifications = new Mock<INotificationRepository>();
        var recipients = new Mock<INotificationRecipientRepository>();
        var id = Guid.NewGuid();
        var envelope = CreateEnvelope(new NotificationRequestModel
        {
            NotificationId = id,
            Title = "title",
            Content = "body",
            Recipients = [new() { UserId = Guid.NewGuid(), IdentitySubject = "subject" }]
        });

        var result = await new NotificationPersistence(notifications.Object, recipients.Object, Mock.Of<IUnitOfWork>(), Mock.Of<Serilog.ILogger>()).PersistAsync(envelope, CancellationToken.None);

        notifications.Verify(x => x.Add(It.Is<Notification>(item => item.Id == id)), Times.Once);
        result.Should().ContainSingle().Which.Notification.NotificationId.Should().Be(id);
    }

    [Fact]
    public async Task PersistAsync_ShouldRejectUpdate_WhenNotificationBelongsToAnotherSourceApp()
    {
        var notifications = new Mock<INotificationRepository>();
        notifications.Setup(x => x.FindFirstByConditionAsync(It.IsAny<Func<IQueryable<Notification>, IQueryable<Notification>>>(), true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Notification { Id = Guid.NewGuid(), SourceApp = "other" });
        var unit = new Mock<IUnitOfWork>();
        var envelope = CreateEnvelope(new NotificationRequestModel
        {
            NotificationId = Guid.NewGuid(),
            Title = "title",
            Content = "body",
            Recipients = [new() { UserId = Guid.NewGuid(), IdentitySubject = "subject" }]
        });

        var action = () => new NotificationPersistence(notifications.Object, Mock.Of<INotificationRecipientRepository>(), unit.Object, Mock.Of<Serilog.ILogger>()).PersistAsync(envelope, CancellationToken.None);

        await action.Should().ThrowAsync<NonRetryableMessageException>();
        unit.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ShouldPublishUpdatedBellAndQueueEmail_WhenNotificationAlreadyExists()
    {
        var userId = Guid.NewGuid();
        var notification = new Notification { Id = Guid.NewGuid(), SourceApp = "app", Title = "old" };
        var recipient = new NotificationRecipient { Id = Guid.NewGuid(), NotificationId = notification.Id, UserId = userId, UserIdentitySubject = "subject" };
        var notifications = new Mock<INotificationRepository>();
        notifications.Setup(x => x.FindFirstByConditionAsync(It.IsAny<Func<IQueryable<Notification>, IQueryable<Notification>>>(), true, It.IsAny<CancellationToken>())).ReturnsAsync(notification);
        var recipients = new Mock<INotificationRecipientRepository>();
        recipients.Setup(x => x.FindAllByConditionAsync(It.IsAny<Func<IQueryable<NotificationRecipient>, IQueryable<NotificationRecipient>>>(), true, It.IsAny<CancellationToken>())).ReturnsAsync([recipient]);
        var templates = new Mock<IEmailTemplateRepository>();
        templates.Setup(x => x.FindFirstByConditionAsync(It.IsAny<Func<IQueryable<EmailTemplate>, IQueryable<EmailTemplate>>>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EmailTemplate { TemplateKey = "template", SubjectTemplate = "subject", BodyTemplate = "body", IsActive = true });
        var publisher = new Mock<IRealtimeNotificationPublisher>();
        var emails = new Mock<IEmailService>();
        var deliveries = new Mock<INotificationDeliveryCommandService>();
        var logger = Mock.Of<Serilog.ILogger>();
        var consumer = new NotificationRequestedConsumer(
            new NotificationPersistence(notifications.Object, recipients.Object, Mock.Of<IUnitOfWork>(), logger),
            new NotificationBellSender(publisher.Object, deliveries.Object, logger),
            new NotificationEmailSender(templates.Object, deliveries.Object, emails.Object, logger),
            Microsoft.Extensions.Options.Options.Create(new SourceAppsOptions { Allowed = ["app"] }));
        var bellPayload = new NotificationRequestModel
        {
            NotificationId = notification.Id,
            Title = "updated",
            Content = "body",
            Recipients = [new() { UserId = userId, IdentitySubject = "subject" }]
        };
        var envelope = new CloudEventEnvelope<UpsertNotificationRequestModel>
        {
            Id = Guid.NewGuid(),
            Source = "gateway",
            Type = "test",
            Data = new UpsertNotificationRequestModel
            {
                SourceApp = "app",
                EventType = "test",
                CreatedById = Guid.NewGuid(),
                Notification = bellPayload,
                Email = new SendEmailFromProducerRequestModel { TemplateKey = "template", Recipients = [new() { UserId = userId, Email = "user@example.com" }] }
            }
        };

        await consumer.HandleAsync(JsonSerializer.SerializeToUtf8Bytes(envelope), CancellationToken.None);

        publisher.Verify(x => x.PublishBellNotificationAsync("app", "subject", It.Is<SendBellNotificationMessage>(message => message.NotificationId == notification.Id && message.Title == "updated"), It.IsAny<CancellationToken>()), Times.Once);
        emails.Verify(x => x.QueueEmailAsync(It.IsAny<TeamNotificationService.Application.Models.Emails.SendEmailToRecipientRequestModel>(), It.IsAny<CancellationToken>()), Times.Once);
        deliveries.Verify(x => x.UpsertDeliveryAsync(It.Is<UpsertNotificationDeliveryRequestModel>(model => model.NotificationRecipientId == recipient.Id && model.Channel == "Email"), It.IsAny<CancellationToken>()), Times.Once);
        notifications.Verify(x => x.Add(It.IsAny<Notification>()), Times.Never);
    }

    private static CloudEventEnvelope<UpsertNotificationRequestModel> CreateEnvelope(NotificationRequestModel notification) => new()
    {
        Id = Guid.NewGuid(),
        Source = "gateway",
        Type = "test",
        Data = new UpsertNotificationRequestModel
        {
            SourceApp = "app",
            EventType = "test",
            CreatedById = Guid.NewGuid(),
            UpdatedById = Guid.NewGuid(),
            Notification = notification
        }
    };

    private static NotificationRequestedConsumer CreateConsumer()
    {
        var logger = Mock.Of<Serilog.ILogger>();
        var deliveries = Mock.Of<INotificationDeliveryCommandService>();
        return new NotificationRequestedConsumer(
            new NotificationPersistence(Mock.Of<INotificationRepository>(), Mock.Of<INotificationRecipientRepository>(), Mock.Of<IUnitOfWork>(), logger),
            new NotificationBellSender(Mock.Of<IRealtimeNotificationPublisher>(), deliveries, logger),
            new NotificationEmailSender(Mock.Of<IEmailTemplateRepository>(), deliveries, Mock.Of<IEmailService>(), logger),
            Microsoft.Extensions.Options.Options.Create(new SourceAppsOptions { Allowed = ["app"] }));
    }
}
