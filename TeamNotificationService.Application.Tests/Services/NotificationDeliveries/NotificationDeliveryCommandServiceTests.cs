using AwesomeAssertions;
using Moq;
using TeamNotificationService.Application.Interfaces.Repositories;
using TeamNotificationService.Application.Models.NotificationDeliveries;
using TeamNotificationService.Application.Services.NotificationDeliveries;
using TeamNotificationService.Domain.Entities;
using TeamNotificationService.Domain.Enums.NotificationDeliveries;
using TeamNotificationService.Domain.Exceptions;
using Xunit;

namespace TeamNotificationService.Application.Tests.Services.NotificationDeliveries;

public class NotificationDeliveryCommandServiceTests
{
    private readonly Mock<INotificationDeliveryRepository> _repository = new();
    private readonly Mock<IUnitOfWork> _unit = new();
    private readonly Mock<Serilog.ILogger> _logger = new();

    [Fact]
    public async Task UpsertDeliveryAsync_ShouldCreateDeliveredRecord_WhenNoRecordExists()
    {
        var recipientId = Guid.NewGuid();
        NotificationDelivery? added = null;
        _repository.Setup(x => x.FindFirstByConditionAsync(It.IsAny<Func<IQueryable<NotificationDelivery>, IQueryable<NotificationDelivery>>>(), true, It.IsAny<CancellationToken>())).ReturnsAsync((NotificationDelivery?)null);
        _repository.Setup(x => x.Add(It.IsAny<NotificationDelivery>())).Callback<NotificationDelivery>(x => added = x).Returns<NotificationDelivery>(x => x);
        _unit.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var result = await Service().UpsertDeliveryAsync(new UpsertNotificationDeliveryRequestModel { NotificationRecipientId = recipientId, Channel = "email", Status = "delivered", Destination = "user@example.test" });

        added.Should().NotBeNull(); added!.Id.Version.Should().Be(7); added.Channel.Should().Be(ChannelType.Email);
        added.DeliveredAt.Should().NotBeNull(); added.FailedAt.Should().BeNull(); result.Status.Should().Be("Delivered");
        _unit.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpsertDeliveryAsync_ShouldIncrementRetryAndTruncateError_WhenDeliveryFails()
    {
        var delivery = new NotificationDelivery { Id = Guid.NewGuid(), Channel = ChannelType.Bell, RetryCount = 2, Destination = "existing" };
        _repository.Setup(x => x.FindFirstByConditionAsync(It.IsAny<Func<IQueryable<NotificationDelivery>, IQueryable<NotificationDelivery>>>(), true, It.IsAny<CancellationToken>())).ReturnsAsync(delivery);
        _unit.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var result = await Service().UpsertDeliveryAsync(new UpsertNotificationDeliveryRequestModel { NotificationRecipientId = Guid.NewGuid(), Channel = "Bell", Status = "Failed", LastError = new string('x', 5001) });

        delivery.RetryCount.Should().Be(3); delivery.LastError.Should().HaveLength(5000);
        delivery.FailedAt.Should().NotBeNull(); result.Destination.Should().Be("existing");
        _repository.Verify(x => x.Add(It.IsAny<NotificationDelivery>()), Times.Never);
    }

    [Fact]
    public async Task UpsertDeliveryAsync_ShouldClearFailureDetails_WhenPreviouslyFailedDeliverySucceeds()
    {
        var delivery = new NotificationDelivery { Id = Guid.NewGuid(), Channel = ChannelType.Bell, Status = DeliveryStatus.Failed, LastError = "offline", FailedAt = DateTimeOffset.UtcNow.AddMinutes(-1), RetryCount = 2 };
        _repository.Setup(x => x.FindFirstByConditionAsync(It.IsAny<Func<IQueryable<NotificationDelivery>, IQueryable<NotificationDelivery>>>(), true, It.IsAny<CancellationToken>())).ReturnsAsync(delivery);
        _unit.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        await Service().UpsertDeliveryAsync(new UpsertNotificationDeliveryRequestModel { NotificationRecipientId = Guid.NewGuid(), Channel = "Bell", Status = "Delivered" });

        delivery.Status.Should().Be(DeliveryStatus.Delivered);
        delivery.FailedAt.Should().BeNull(); delivery.LastError.Should().BeNull();
        delivery.DeliveredAt.Should().NotBeNull(); delivery.SentAt.Should().NotBeNull();
        delivery.RetryCount.Should().Be(2);
    }

    [Fact]
    public async Task UpsertDeliveryAsync_ShouldRejectInvalidChannel_WhenInputIsInvalid()
    {
        var action = () => Service().UpsertDeliveryAsync(new UpsertNotificationDeliveryRequestModel { NotificationRecipientId = Guid.NewGuid(), Channel = "invalid", Status = "Delivered" });
        await action.Should().ThrowAsync<UnprocessableEntityException>();
        _unit.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    private NotificationDeliveryCommandService Service() => new(_logger.Object, _repository.Object, _unit.Object);
}
