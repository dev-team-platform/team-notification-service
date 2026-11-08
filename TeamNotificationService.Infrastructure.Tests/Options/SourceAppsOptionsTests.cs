using System.Security.Claims;
using System.Text.Json;
using AwesomeAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Moq;
using TeamNotificationService.Domain.Exceptions;
using TeamNotificationService.Infrastructure.Contexts;
using TeamNotificationService.Infrastructure.Options;
using TeamNotificationService.Infrastructure.Services.Email;
using Xunit;

namespace TeamNotificationService.Infrastructure.Tests.Options;

public class SourceAppsOptionsTests
{
    [Theory]
    [InlineData("organization", true)]
    [InlineData("Organization", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void IsAllowed_ShouldMatchConfiguredSourceCaseSensitively_WhenSourceIsProvided(string? source, bool expected) =>
        new SourceAppsOptions { Allowed = ["organization"] }.IsAllowed(source).Should().Be(expected);
}
