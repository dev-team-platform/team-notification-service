using TeamNotificationService.Application.Models.Notifications;

namespace TeamNotificationService.Application.Interfaces.Services.Notifications;

public interface INotificationCommandService
{
    Task<MarkNotificationReadResponseModel> MarkNotificationReadAsync(
        MarkNotificationReadRequestModel requestModel,
        CancellationToken cancellationToken = default);
}
