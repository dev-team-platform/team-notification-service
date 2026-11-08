using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using TeamNotificationService.Domain.Entities;
using TeamNotificationService.Infrastructure.Persistence;
using TeamNotificationService.Infrastructure.Repositories;
using Xunit;

namespace TeamNotificationService.Infrastructure.Tests.Repositories;

public class GenericRepositoryTests
{
    [Fact]
    public void Add_ShouldTrackEntityWithoutSaving_WhenEntityIsAdded()
    {
        using var context = CreateContext();
        var repository = new NotificationDeliveryRepository(Mock.Of<Serilog.ILogger>(), context);
        var delivery = new NotificationDelivery { Id = Guid.NewGuid(), NotificationRecipientId = Guid.NewGuid() };

        var result = repository.Add(delivery);

        result.Should().BeSameAs(delivery);
        context.Entry(delivery).State.Should().Be(EntityState.Added);
    }

    [Fact]
    public void AddRange_ShouldTrackAllEntities_WhenMultipleItemsAreAdded()
    {
        using var context = CreateContext();
        var repository = new EmailTemplateRepository(Mock.Of<Serilog.ILogger>(), context);
        var templates = new[] { new EmailTemplate { Id = Guid.NewGuid() }, new EmailTemplate { Id = Guid.NewGuid() } };

        var result = repository.AddRange(templates);

        result.Should().BeSameAs(templates);
        context.ChangeTracker.Entries<EmailTemplate>().Should().HaveCount(2).And.OnlyContain(entry => entry.State == EntityState.Added);
    }

    [Fact]
    public void QueryByCondition_ShouldApplyPredicate_WhenQueryIsTranslated()
    {
        using var context = CreateContext();
        var repository = new EmailTemplateRepository(Mock.Of<Serilog.ILogger>(), context);

        var sql = repository.QueryByCondition(query => query.Where(item => item.IsActive && item.TemplateKey == "welcome")).ToQueryString();

        sql.Should().Contain("WHERE").And.Contain("is_active").And.Contain("template_key").And.Contain("welcome");
        context.ChangeTracker.Entries().Should().BeEmpty();
    }

    private static AppDbContext CreateContext() => new(new DbContextOptionsBuilder<AppDbContext>()
        .UseNpgsql("Host=localhost;Database=test;Username=test;Password=test").Options);
}
