using AwesomeAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using TeamNotificationService.Api.Controllers;
using TeamNotificationService.Application.Interfaces.Services;
using Xunit;

namespace TeamNotificationService.Api.Tests.Controllers;

public class HealthCheckControllerTests
{
    [Theory]
    [InlineData(true, 200)]
    [InlineData(false, 503)]
    public async Task GetDbHealth_ShouldReturnMatchingStatus_WhenRepositoryHealthIsKnown(bool healthy, int status)
    {
        var health = new Mock<IHealthCheckService>();
        health.Setup(x => x.HealthCheckAsync(It.IsAny<CancellationToken>())).ReturnsAsync(healthy);

        var result = (await new HealthCheckController(health.Object).GetDbHealth()).Should().BeAssignableTo<ObjectResult>().Which;

        result.StatusCode.Should().Be(status);
    }

}
