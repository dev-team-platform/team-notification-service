using TeamNotificationService.Application.Models.Emails;

namespace TeamNotificationService.Application.Interfaces.Services.Emails;

/// <summary>
/// Queues email for asynchronous delivery by Notification Service.
/// </summary>
public interface IEmailService
{
    Task QueueEmailAsync(
        SendEmailToRecipientRequestModel message,
        CancellationToken cancellationToken = default);
}
