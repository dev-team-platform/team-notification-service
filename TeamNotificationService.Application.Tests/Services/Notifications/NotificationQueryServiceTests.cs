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

public class NotificationQueryServiceTests
{
    private readonly Mock<ICurrentUserContext> _user = new();
    private readonly Mock<INotificationRepository> _notifications = new();

    public NotificationQueryServiceTests() => _user.SetupGet(x => x.IdentitySubject).Returns("subject");

    [Fact]
    public async Task GetLatestNotificationForUserAsync_ShouldUseCurrentSubjectAndRequestFilters_WhenCalled()
    {
        var list = new List<GetLatestNotificationForUserResponseModel>();
        using var cancellation = new CancellationTokenSource();
        _notifications.Setup(x => x.FindLatestForUserAsync("subject", "app", true, cancellation.Token)).ReturnsAsync(list);
        var service = new NotificationQueryService(_user.Object, _notifications.Object);

        var result = await service.GetLatestNotificationForUserAsync(new GetLatestNotificationForUserRequestModel { SourceApp = "app", IsUnreadOnly = true }, cancellation.Token);

        result.Should().BeSameAs(list);
    }

    [Fact]
    public async Task GetAllNotificationsAsync_ShouldPreservePagination_WhenRepositoryReturnsResults()
    {
        var model = new GetAllNotificationsRequestModel { SourceApp = "app", CurrentPage = 2, ItemsPerPage = 10 };
        var page = new FilterResult<GetAllNotificationsResponseModelItem> { Items = [new()], CurrentPage = 2, ItemsPerPage = 10, TotalItems = 11, TotalPages = 2 };
        _notifications.Setup(x => x.FindAllForUserAsync("subject", "app", model, It.IsAny<CancellationToken>())).ReturnsAsync(page);

        var result = await new NotificationQueryService(_user.Object, _notifications.Object).GetAllNotificationsAsync(model);

        result.Items.Should().BeSameAs(page.Items); result.TotalItems.Should().Be(11); result.CurrentPage.Should().Be(2);
    }

    [Fact]
    public async Task GetLatestNotificationForUserAsync_ShouldRejectNullRequest_WhenCalled()
    {
        var service = new NotificationQueryService(_user.Object, _notifications.Object);

        var action = () => service.GetLatestNotificationForUserAsync(null!);

        await action.Should().ThrowAsync<ArgumentNullException>();
        _notifications.Verify(x => x.FindLatestForUserAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
    }

}

