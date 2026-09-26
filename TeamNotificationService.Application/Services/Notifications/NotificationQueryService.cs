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

    public Task<IReadOnlyList<GetUnreadNotificationsResponseModel>> GetUnreadNotificationsAsync(
        CancellationToken cancellationToken = default)
    {
        return _notificationRepository.FindUnreadForUserAsync(
            _currentUserContext.IdentitySubject,
            cancellationToken);
    }

    public async Task<GetAllNotificationsReponseModel> GetAllNotificationsAsync(
        GetAllNotificationsRequestModel model,
        CancellationToken cancellationToken = default)
    {
        var result = await _notificationRepository.FindAllForUserAsync(
            _currentUserContext.IdentitySubject,
            model,
            cancellationToken);

        return GetAllNotificationsReponseModel.FromFilterResult(result);
    }
}
