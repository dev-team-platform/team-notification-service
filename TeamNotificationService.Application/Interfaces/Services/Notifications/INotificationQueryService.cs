using TeamNotificationService.Application.Models.Notifications;

namespace TeamNotificationService.Application.Interfaces.Services.Notifications;

public interface INotificationQueryService
{
    Task<IReadOnlyList<GetLatelyNotificationForUserResponseModel>> GetLatelyNotificationForUserAsync(
        GetLatelyNotificationForUserRequestModel model,
        CancellationToken cancellationToken = default);

    Task<GetAllNotificationsReponseModel> GetAllNotificationsAsync(
        GetAllNotificationsRequestModel model,
        CancellationToken cancellationToken = default);
}
