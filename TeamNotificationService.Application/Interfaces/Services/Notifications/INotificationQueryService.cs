using TeamNotificationService.Application.Models.Notifications;

namespace TeamNotificationService.Application.Interfaces.Services.Notifications;

public interface INotificationQueryService
{
    Task<IReadOnlyList<GetLatestNotificationForUserResponseModel>> GetLatestNotificationForUserAsync(
        GetLatestNotificationForUserRequestModel model,
        CancellationToken cancellationToken = default);

    Task<GetAllNotificationsReponseModel> GetAllNotificationsAsync(
        GetAllNotificationsRequestModel model,
        CancellationToken cancellationToken = default);
}
