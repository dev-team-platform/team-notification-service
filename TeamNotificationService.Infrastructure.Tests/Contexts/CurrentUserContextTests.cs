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

namespace TeamNotificationService.Infrastructure.Tests.Contexts;

public class CurrentUserContextTests
{
    [Fact]
    public void IdentitySubject_ShouldReturnSubClaim_WhenPresent()
    {
        var context = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", "user-1")], "test")) };
        var accessor = new HttpContextAccessor { HttpContext = context };
        new CurrentUserContext(accessor, Mock.Of<IHostEnvironment>()).IdentitySubject.Should().Be("user-1");
    }

    [Fact]
    public void IdentitySubject_ShouldThrowUnauthorized_WhenClaimIsMissing()
    {
        var action = () => new CurrentUserContext(new HttpContextAccessor { HttpContext = new DefaultHttpContext() }, Mock.Of<IHostEnvironment>()).IdentitySubject;
        action.Should().Throw<UnauthorizedException>();
    }
}
