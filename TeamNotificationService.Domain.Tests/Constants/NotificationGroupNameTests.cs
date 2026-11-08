using AwesomeAssertions;
using TeamNotificationService.Domain.Constants;
using TeamNotificationService.Domain.Enums.NotificationDeliveries;
using TeamNotificationService.Domain.Exceptions;
using TeamNotificationService.Domain.Utils;
using Xunit;

namespace TeamNotificationService.Domain.Tests.Constants;

public class NotificationGroupNameTests
{
    [Fact]
    public void ForUser_ShouldIncludeSourceAndSubject_WhenBuildingGroupName() =>
        NotificationGroupName.ForUser("organization", "user-1").Should().Be("organization:user-1");
}
