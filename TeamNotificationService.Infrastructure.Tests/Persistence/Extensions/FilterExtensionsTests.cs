using AwesomeAssertions;
using TeamNotificationService.Application.Models.Common;
using TeamNotificationService.Application.Models.Notifications;
using TeamNotificationService.Infrastructure.Persistence.Extensions;
using Xunit;

namespace TeamNotificationService.Infrastructure.Tests.Persistence.Extensions;

public class FilterExtensionsTests
{
    [Fact]
    public void ApplySorting_ShouldOrderBySentAt_WhenRequested()
    {
        var now = DateTimeOffset.UtcNow;
        var items = new[]
        {
            new GetAllNotificationsResponseModelItem { Title = "old", SentAt = now.AddDays(-2) },
            new GetAllNotificationsResponseModelItem { Title = "resent", SentAt = now },
            new GetAllNotificationsResponseModelItem { Title = "new", SentAt = now.AddDays(-1) }
        }.AsQueryable();
        var request = new GetAllNotificationsRequestModel
        {
            SourceApp = "app",
            SortFields = [new SortField { FieldName = "sentAt", IsAscending = false }]
        };

        var result = items.ApplySorting(request).Select(item => item.Title).ToArray();

        result.Should().Equal("resent", "new", "old");
    }

}
