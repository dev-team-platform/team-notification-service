using TeamNotificationService.Application.Models.Common;
using TeamNotificationService.Application.Models.Notifications;
using TeamNotificationService.Domain.Entities;

namespace TeamNotificationService.Application.Interfaces.Repositories;

public interface INotificationRepository : IGenericRepository<Notification>
{
    Task<IReadOnlyList<GetUnreadNotificationsResponseModel>> FindUnreadForUserAsync(
        string userIdentitySubject,
        CancellationToken cancellationToken = default);

    Task<FilterResult<GetAllNotificationsResponseModelItem>> FindAllForUserAsync(
        string userIdentitySubject,
        FilterQuery<GetAllNotificationsResponseModelItem> query,
        CancellationToken cancellationToken = default);
}
