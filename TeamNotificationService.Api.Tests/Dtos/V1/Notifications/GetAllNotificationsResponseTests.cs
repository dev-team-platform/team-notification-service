using AwesomeAssertions;
using TeamNotificationService.Api.Dtos.V1.Notifications;
using TeamNotificationService.Application.Models.Notifications;
using Xunit;

namespace TeamNotificationService.Api.Tests.Dtos.V1.Notifications;

public class GetAllNotificationsResponseTests
{
    [Fact]
    public void FromModel_ShouldExposeSentAt_WhenMappingNotification()
    {
        var sentAt = DateTimeOffset.UtcNow;

        var result = GetAllNotificationsResponseItem.FromModel(new GetAllNotificationsResponseModelItem { SentAt = sentAt });

        result.SentAt.Should().Be(sentAt);
        var json = System.Text.Json.JsonSerializer.Serialize(result);
        json.Should().Contain("\"SentAt\"").And.NotContain("\"CreatedAt\"");
    }

}
