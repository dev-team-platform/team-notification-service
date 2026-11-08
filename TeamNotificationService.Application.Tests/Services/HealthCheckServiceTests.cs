using AwesomeAssertions;
using Moq;
using TeamNotificationService.Application.Interfaces.Repositories;
using TeamNotificationService.Application.Services;
using Xunit;

namespace TeamNotificationService.Application.Tests.Services;

public class HealthCheckServiceTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task HealthCheckAsync_ShouldReturnRepositoryResult_WhenCalled(bool healthy)
    {
        using var cancellation = new CancellationTokenSource();
        var repository = new Mock<IHealthCheckRepository>();
        repository.Setup(x => x.HealthCheckAsync(cancellation.Token)).ReturnsAsync(healthy);

        var result = await new HealthCheckService(repository.Object).HealthCheckAsync(cancellation.Token);

        result.Should().Be(healthy);
        repository.Verify(x => x.HealthCheckAsync(cancellation.Token), Times.Once);
    }

    [Fact]
    public async Task HealthCheckAsync_ShouldPropagateFailure_WhenRepositoryFails()
    {
        var repository = new Mock<IHealthCheckRepository>();
        repository.Setup(x => x.HealthCheckAsync(It.IsAny<CancellationToken>())).ThrowsAsync(new InvalidOperationException("database unavailable"));

        var action = () => new HealthCheckService(repository.Object).HealthCheckAsync();

        await action.Should().ThrowAsync<InvalidOperationException>().WithMessage("database unavailable");
    }
}
