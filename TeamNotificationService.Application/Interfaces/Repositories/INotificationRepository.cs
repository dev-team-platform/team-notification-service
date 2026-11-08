using TeamNotificationService.Application.Models.Common;
using TeamNotificationService.Application.Models.Notifications;
using TeamNotificationService.Domain.Entities;

namespace TeamNotificationService.Application.Interfaces.Repositories;

public interface INotificationRepository : IGenericRepository<Notification>
{
    Task<IReadOnlyList<GetLatestNotificationForUserResponseModel>> FindLatestForUserAsync(
        string userIdentitySubject,
        string sourceApp,
        bool isUnreadOnly,
        CancellationToken cancellationToken = default);

    Task<FilterResult<GetAllNotificationsResponseModelItem>> FindAllForUserAsync(
        string userIdentitySubject,
        string sourceApp,
        FilterQuery<GetAllNotificationsResponseModelItem> query,
        CancellationToken cancellationToken = default);
}
