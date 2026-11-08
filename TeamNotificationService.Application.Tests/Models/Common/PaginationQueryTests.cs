using AwesomeAssertions;
using TeamNotificationService.Application.Models.Common;
using TeamNotificationService.Domain.Exceptions;
using Xunit;

namespace TeamNotificationService.Application.Tests.Models.Common;

public class PaginationQueryTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void CurrentPage_ShouldRejectNonPositiveValues_WhenQueryIsCreated(int value)
    {
        var action = () => new PaginationQuery { CurrentPage = value };
        action.Should().Throw<UnprocessableEntityException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void ItemsPerPage_ShouldRejectNonPositiveValues_WhenQueryIsCreated(int value)
    {
        var action = () => new PaginationQuery { ItemsPerPage = value };
        action.Should().Throw<UnprocessableEntityException>();
    }

    [Fact]
    public void Constructor_ShouldSetDefaults_WhenNoPaginationIsProvided()
    {
        var query = new PaginationQuery();
        query.CurrentPage.Should().Be(1);
        query.ItemsPerPage.Should().Be(int.MaxValue);
    }
}
