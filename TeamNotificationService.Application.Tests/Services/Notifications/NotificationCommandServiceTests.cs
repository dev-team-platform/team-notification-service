using AwesomeAssertions;
using Moq;
using TeamNotificationService.Application.Interfaces.Contexts;
using TeamNotificationService.Application.Interfaces.Repositories;
using TeamNotificationService.Application.Models.Common;
using TeamNotificationService.Application.Models.Notifications;
using TeamNotificationService.Application.Services.Notifications;
using TeamNotificationService.Domain.Entities;
using TeamNotificationService.Domain.Exceptions;
using Xunit;

namespace TeamNotificationService.Application.Tests.Services.Notifications;

public class NotificationCommandServiceTests
{
    private readonly Mock<ICurrentUserContext> _user = new();
    private readonly Mock<INotificationRecipientRepository> _recipients = new();
    private readonly Mock<IUnitOfWork> _unit = new();

    public NotificationCommandServiceTests() => _user.SetupGet(x => x.IdentitySubject).Returns("subject");

    [Fact]
    public async Task MarkNotificationReadAsync_ShouldMarkOnlyUnreadRecords_WhenSpecificIdsAreProvided()
    {
        var id1 = Guid.NewGuid(); var id2 = Guid.NewGuid();
        var unread = new NotificationRecipient { NotificationId = id1 };
        var readAt = DateTimeOffset.UtcNow.AddDays(-1);
        var read = new NotificationRecipient { NotificationId = id2, IsRead = true, ReadAt = readAt };
        _recipients.Setup(x => x.FindForUserAndSourceAppAsync("subject", "app", It.IsAny<IReadOnlyCollection<Guid>>(), false, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync([unread, read]);
        _unit.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var result = await Command().MarkNotificationReadAsync(new MarkNotificationReadRequestModel { SourceApp = "app", NotificationIds = [id1, id1, id2] });

        result.MarkedCount.Should().Be(1);
        unread.IsRead.Should().BeTrue(); unread.ReadAt.Should().NotBeNull();
        read.ReadAt.Should().Be(readAt);
        _recipients.Verify(x => x.FindForUserAndSourceAppAsync("subject", "app", It.Is<IReadOnlyCollection<Guid>>(ids => ids.Count == 2 && ids.Contains(id1) && ids.Contains(id2)), false, true, It.IsAny<CancellationToken>()), Times.Once);
        _unit.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task MarkNotificationReadAsync_ShouldMarkAllUnreadForCurrentUser_WhenIsAllIsTrue()
    {
        var one = new NotificationRecipient(); var two = new NotificationRecipient();
        _recipients.Setup(x => x.FindForUserAndSourceAppAsync("subject", "app", null, true, true, It.IsAny<CancellationToken>())).ReturnsAsync([one, two]);
        _unit.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var result = await Command().MarkNotificationReadAsync(new MarkNotificationReadRequestModel { SourceApp = "app", IsAll = true });

        result.MarkedCount.Should().Be(2);
        one.IsRead.Should().BeTrue(); two.IsRead.Should().BeTrue();
        one.ReadAt.Should().Be(two.ReadAt);
    }

    [Fact]
    public async Task MarkNotificationReadAsync_ShouldRejectEmptyIds_WhenIsAllIsFalse()
    {
        var action = () => Command().MarkNotificationReadAsync(new MarkNotificationReadRequestModel { SourceApp = "app" });
        await action.Should().ThrowAsync<UnprocessableEntityException>();
        _unit.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task MarkNotificationReadAsync_ShouldRejectAllChanges_WhenAnyIdIsNotFoundForUser()
    {
        var item = new NotificationRecipient();
        _recipients.Setup(x => x.FindForUserAndSourceAppAsync("subject", "app", It.IsAny<IReadOnlyCollection<Guid>>(), false, true, It.IsAny<CancellationToken>())).ReturnsAsync([item]);
        var action = () => Command().MarkNotificationReadAsync(new MarkNotificationReadRequestModel { SourceApp = "app", NotificationIds = [Guid.NewGuid(), Guid.NewGuid()] });
        await action.Should().ThrowAsync<NotFoundException>();
        item.IsRead.Should().BeFalse();
        _unit.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }


    private NotificationCommandService Command() => new(_user.Object, _recipients.Object, _unit.Object);
}
