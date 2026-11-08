using AwesomeAssertions;
using Moq;
using TeamNotificationService.Application.Models.Emails;
using TeamNotificationService.Infrastructure.Options;
using TeamNotificationService.Infrastructure.Services.Email;
using Xunit;

namespace TeamNotificationService.Infrastructure.Tests.Services.Email;

public class EmailServiceTests
{
    [Theory]
    [InlineData(null, "subject", "body")]
    [InlineData("user@example.test", "", "body")]
    [InlineData("user@example.test", "subject", " ")]
    public void QueueEmailAsync_ShouldRejectMissingFields_WhenMessageIsInvalid(string? to, string subject, string body)
    {
        using var service = CreateService();
        var message = new SendEmailToRecipientRequestModel { ToEmail = to!, Subject = subject, Body = body };

        Action action = () => service.QueueEmailAsync(message);

        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public async Task QueueEmailAsync_ShouldHonorCancellation_WhenQueueIsFull()
    {
        using var service = CreateService();
        var message = new SendEmailToRecipientRequestModel { ToEmail = "user@example.test", Subject = "subject", Body = "body" };
        await service.QueueEmailAsync(message);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        var action = () => service.QueueEmailAsync(message, cancellation.Token);

        await action.Should().ThrowAsync<OperationCanceledException>();
    }

    private static EmailService CreateService() => new(Mock.Of<Serilog.ILogger>(),
        Microsoft.Extensions.Options.Options.Create(new EmailOptions { QueueCapacity = 1 }));
}
