using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using TeamNotificationService.Domain.Entities;
using TeamNotificationService.Infrastructure.Persistence;
using TeamNotificationService.Infrastructure.Repositories;
using Xunit;

namespace TeamNotificationService.Infrastructure.Tests.Repositories;

public class NotificationRepositoryTests
{
    [Fact]
    public void BuildLatestForUserQuery_ShouldUseRecipientSentTime_WhenFilteringAndOrdering()
    {
        var now = DateTimeOffset.UtcNow;
        var old = new Notification { Id = Guid.NewGuid(), SourceApp = "app", Title = "old" };
        var resent = new Notification { Id = Guid.NewGuid(), SourceApp = "app", Title = "resent" };
        var recent = new Notification { Id = Guid.NewGuid(), SourceApp = "app", Title = "recent" };
        var read = new Notification { Id = Guid.NewGuid(), SourceApp = "app", Title = "read" };
        var notifications = new[] { old, resent, recent, read }.AsQueryable();
        var recipients = new[]
        {
            new NotificationRecipient { NotificationId = old.Id, UserIdentitySubject = "subject", CreatedAt = now.AddDays(-20) },
            new NotificationRecipient { NotificationId = resent.Id, UserIdentitySubject = "subject", CreatedAt = now.AddDays(-20), UpdatedAt = now },
            new NotificationRecipient { NotificationId = recent.Id, UserIdentitySubject = "subject", CreatedAt = now.AddDays(-1) },
            new NotificationRecipient { NotificationId = read.Id, UserIdentitySubject = "subject", CreatedAt = now, IsRead = true }
        }.AsQueryable();

        var result = NotificationRepository.BuildLatestForUserQuery(recipients, notifications, "subject", "app", true, now.AddDays(-10)).ToList();

        result.Select(item => item.Title).Should().Equal("resent", "recent");
        result[0].SentAt.Should().Be(now);
        result[1].SentAt.Should().Be(now.AddDays(-1));
    }

    [Fact]
    public void BuildLatestForUserQuery_ShouldTranslateToSql_WhenUsingPostgres()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql("Host=localhost;Database=test;Username=test;Password=test")
            .Options;
        using var context = new AppDbContext(options);

        var query = NotificationRepository.BuildLatestForUserQuery(
            context.NotificationRecipients, context.Notifications, "subject", "app", true, DateTimeOffset.UtcNow.AddDays(-10));

        var sql = query.ToQueryString();
        sql.Should().Contain("COALESCE").And.Contain("updated_at").And.Contain("created_at").And.Contain("ORDER BY");
    }
}
