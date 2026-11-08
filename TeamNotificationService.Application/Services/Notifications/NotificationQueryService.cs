using TeamNotificationService.Application.Interfaces.Contexts;
using TeamNotificationService.Application.Interfaces.Repositories;
using TeamNotificationService.Application.Interfaces.Services.Notifications;
using TeamNotificationService.Application.Models.Notifications;

namespace TeamNotificationService.Application.Services.Notifications;

public class NotificationQueryService : INotificationQueryService
{
    private readonly ICurrentUserContext _currentUserContext;
    private readonly INotificationRepository _notificationRepository;

    public NotificationQueryService(
        ICurrentUserContext currentUserContext,
        INotificationRepository notificationRepository)
    {
        _currentUserContext = currentUserContext;
        _notificationRepository = notificationRepository;
    }

    public Task<IReadOnlyList<GetLatestNotificationForUserResponseModel>> GetLatestNotificationForUserAsync(
        GetLatestNotificationForUserRequestModel model,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(model);

        return _notificationRepository.FindLatestForUserAsync(
            _currentUserContext.IdentitySubject,
            model.SourceApp,
            model.IsUnreadOnly,
            cancellationToken);
    }

    public async Task<GetAllNotificationsReponseModel> GetAllNotificationsAsync(
        GetAllNotificationsRequestModel model,
        CancellationToken cancellationToken = default)
    {
        var result = await _notificationRepository.FindAllForUserAsync(
            _currentUserContext.IdentitySubject,
            model.SourceApp,
            model,
            cancellationToken);

        return GetAllNotificationsReponseModel.FromFilterResult(result);
    }
}
