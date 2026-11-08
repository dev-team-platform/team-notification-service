using System.Reflection;
using AwesomeAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using TeamNotificationService.Api.Controllers;
using TeamNotificationService.Api.V1.Controllers;
using Xunit;

namespace TeamNotificationService.Api.Tests.Controllers;

public class RateLimitingRemovalTests
{
    [Theory]
    [InlineData(typeof(NotificationController))]
    [InlineData(typeof(HealthCheckController))]
    public void Controllers_ShouldNotDeclareLocalRateLimits_WhenMapped(Type controller)
    {
        controller.GetCustomAttributes<EnableRateLimitingAttribute>(true).Should().BeEmpty();
        controller.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .SelectMany(method => method.GetCustomAttributes<EnableRateLimitingAttribute>(true))
            .Should().BeEmpty();
    }

    [Fact]
    public void NotificationController_ShouldRequireAuthentication_WhenMapped()
    {
        typeof(NotificationController).GetCustomAttributes<AuthorizeAttribute>(true).Should().NotBeEmpty();
    }
}
