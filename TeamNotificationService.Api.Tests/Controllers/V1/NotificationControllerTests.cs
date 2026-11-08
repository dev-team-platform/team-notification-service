using AwesomeAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using TeamNotificationService.Api.Controllers;
using TeamNotificationService.Api.Dtos.V1.Notifications;
using TeamNotificationService.Api.V1.Controllers;
using TeamNotificationService.Application.Interfaces.Services;
using TeamNotificationService.Application.Interfaces.Services.Notifications;
using TeamNotificationService.Application.Models.Notifications;
using TeamNotificationService.Infrastructure.Options;
using Xunit;

namespace TeamNotificationService.Api.Tests.Controllers.V1;

public class NotificationControllerTests
{
    private readonly Mock<INotificationQueryService> _query = new();
    private readonly Mock<INotificationCommandService> _command = new();

    [Fact]
    public async Task GetLatestNotificationForUserAsync_ShouldReturnMappedResponse_WhenSourceIsAllowed()
    {
        var id = Guid.NewGuid();
        var sentAt = DateTimeOffset.UtcNow;
        using var cancellation = new CancellationTokenSource();
        _query.Setup(x => x.GetLatestNotificationForUserAsync(It.IsAny<GetLatestNotificationForUserRequestModel>(), cancellation.Token))
            .ReturnsAsync([new GetLatestNotificationForUserResponseModel { NotificationId = id, Title = "Hello", SentAt = sentAt }]);

        var result = (await Controller().GetLatestNotificationForUserAsync(new GetLatestNotificationForUserRequest { SourceApp = "app", IsUnreadOnly = true }, cancellation.Token)).Should().BeOfType<OkObjectResult>().Which;

        var notification = result.Value.Should().BeAssignableTo<IReadOnlyList<GetLatestNotificationForUserResponse>>().Which.Should().ContainSingle().Which;
        notification.NotificationId.Should().Be(id);
        notification.SentAt.Should().Be(sentAt);
        var json = System.Text.Json.JsonSerializer.Serialize(notification);
        json.Should().Contain("\"SentAt\"").And.NotContain("\"CreatedAt\"");
        _query.Verify(x => x.GetLatestNotificationForUserAsync(It.Is<GetLatestNotificationForUserRequestModel>(model => model.SourceApp == "app" && model.IsUnreadOnly), cancellation.Token), Times.Once);
    }

    [Fact]
    public async Task MarkNotificationReadAsync_ShouldReturnCountAndDistinctIds_WhenSourceIsAllowed()
    {
        var id = Guid.NewGuid();
        _command.Setup(x => x.MarkNotificationReadAsync(It.IsAny<MarkNotificationReadRequestModel>(), It.IsAny<CancellationToken>())).ReturnsAsync(new MarkNotificationReadResponseModel { MarkedCount = 1 });

        var result = (await Controller().MarkNotificationReadAsync(new MarkNotificationReadRequest { SourceApp = "app", NotificationIds = [id, id] })).Should().BeOfType<OkObjectResult>().Which;

        result.Value.Should().BeOfType<MarkNotificationReadResponse>().Which.MarkedCount.Should().Be(1);
        _command.Verify(x => x.MarkNotificationReadAsync(It.Is<MarkNotificationReadRequestModel>(model => model.NotificationIds.Count == 1 && model.NotificationIds[0] == id), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetLatestNotificationForUserAsync_ShouldReturnBadRequest_WhenSourceIsNotAllowed()
    {
        var controller = Controller();
        var services = new ServiceCollection();
        services.AddLogging(); services.AddControllers();
        using var provider = services.BuildServiceProvider();
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { RequestServices = provider } };

        var result = await controller.GetLatestNotificationForUserAsync(new GetLatestNotificationForUserRequest { SourceApp = "unknown" });

        result.Should().BeOfType<BadRequestObjectResult>().Which.StatusCode.Should().Be(400);
        _query.Verify(x => x.GetLatestNotificationForUserAsync(It.IsAny<GetLatestNotificationForUserRequestModel>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private NotificationController Controller() => new(_query.Object, _command.Object,
        Microsoft.Extensions.Options.Options.Create(new SourceAppsOptions { Allowed = ["app"] }));
}
