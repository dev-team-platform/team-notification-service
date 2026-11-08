using System.Data;
using System.Data.Common;
using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Moq;
using TeamNotificationService.Infrastructure.Persistence;
using TeamNotificationService.Infrastructure.Repositories;
using Xunit;

namespace TeamNotificationService.Infrastructure.Tests.Repositories;

public class NotificationRecipientRepositoryTests
{
    [Fact]
    public async Task FindForUserAndSourceAppAsync_ShouldIncludeRequestedFilters_WhenIdsAndUnreadOnlyAreProvided()
    {
        var interceptor = new QueryCaptureInterceptor();
        using var context = CreateContext(interceptor);
        var repository = new NotificationRecipientRepository(Mock.Of<Serilog.ILogger>(), context);
        var notificationId = Guid.NewGuid();

        var result = await repository.FindForUserAndSourceAppAsync("subject", "app", [notificationId], unreadOnly: true);

        result.Should().BeEmpty();
        interceptor.Sql.Should().NotBeNull().And.Contain("JOIN").And.Contain("user_identity_subject")
            .And.Contain("source_app").And.Contain("is_read").And.Contain("notification_id");
        interceptor.Parameters.Should().Contain("subject").And.Contain("app");
        context.ChangeTracker.Entries().Should().BeEmpty();
    }

    [Fact]
    public async Task FindForUserAndSourceAppAsync_ShouldOmitOptionalFilters_WhenTheyAreNotRequested()
    {
        var interceptor = new QueryCaptureInterceptor();
        using var context = CreateContext(interceptor);
        var repository = new NotificationRecipientRepository(Mock.Of<Serilog.ILogger>(), context);

        await repository.FindForUserAndSourceAppAsync("subject", "app");

        interceptor.Sql.Should().NotContain("NOT (n.is_read)").And.NotContain(" = ANY");
        interceptor.Parameters.Should().Contain("subject").And.Contain("app");
    }

    private static AppDbContext CreateContext(QueryCaptureInterceptor interceptor) => new(new DbContextOptionsBuilder<AppDbContext>()
        .UseNpgsql("Host=localhost;Database=test;Username=test;Password=test")
        .AddInterceptors(interceptor, new SuppressConnectionInterceptor()).Options);

    private sealed class QueryCaptureInterceptor : DbCommandInterceptor
    {
        public string? Sql { get; private set; }
        public List<object?> Parameters { get; } = [];

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
        {
            Sql = command.CommandText;
            Parameters.AddRange(command.Parameters.Cast<DbParameter>().Select(parameter => parameter.Value));
            var table = new DataTable();
            foreach (var column in new[] { "id", "notification_id", "user_id", "user_identity_subject", "is_read", "read_at", "created_at", "updated_at" })
                table.Columns.Add(column);
            return ValueTask.FromResult(InterceptionResult<DbDataReader>.SuppressWithResult(table.CreateDataReader()));
        }

    }

    private sealed class SuppressConnectionInterceptor : DbConnectionInterceptor
    {
        public override ValueTask<InterceptionResult> ConnectionOpeningAsync(
            DbConnection connection, ConnectionEventData eventData, InterceptionResult result, CancellationToken cancellationToken = default)
            => ValueTask.FromResult(InterceptionResult.Suppress());
    }
}
