using AwesomeAssertions;
using Microsoft.AspNetCore.Http;
using Moq;
using TeamNotificationService.Api.Middlewares;
using TeamNotificationService.Domain.Exceptions;
using Xunit;

namespace TeamNotificationService.Api.Tests.Middlewares;

public class GlobalExceptionHandlerMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_ShouldPassContextToNextDelegate_WhenRequestSucceeds()
    {
        var context = new DefaultHttpContext();
        HttpContext? received = null;
        var middleware = new GlobalExceptionHandlerMiddleware(
            current => { received = current; return Task.CompletedTask; },
            Mock.Of<Serilog.ILogger>());

        await middleware.InvokeAsync(context);

        received.Should().BeSameAs(context);
    }

    [Theory]
    [InlineData("unauthorized", 401)]
    [InlineData("forbidden", 403)]
    [InlineData("notfound", 404)]
    [InlineData("conflict", 409)]
    [InlineData("unprocessable", 422)]
    [InlineData("unknown", 500)]
    public async Task InvokeAsync_ShouldMapExceptionToStatus_WhenRequestFails(string kind, int status)
    {
        Exception error = kind switch
        {
            "unauthorized" => new UnauthorizedException("error"),
            "forbidden" => new ForbiddenException("error"),
            "notfound" => new NotFoundException("error"),
            "conflict" => new ConflictException("error"),
            "unprocessable" => new UnprocessableEntityException("error"),
            _ => new InvalidOperationException("sensitive detail")
        };
        var context = new DefaultHttpContext(); context.Response.Body = new MemoryStream();
        var middleware = new GlobalExceptionHandlerMiddleware(_ => throw error, Mock.Of<Serilog.ILogger>());

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(status);
        context.Response.Body.Position = 0;
        var body = await new StreamReader(context.Response.Body).ReadToEndAsync();
        body.Should().Contain(status == 500 ? "Something went wrong." : "error");
        if (status == 500) body.Should().NotContain("sensitive detail");
    }
}
