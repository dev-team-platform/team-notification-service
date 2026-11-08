using TeamNotificationService.Domain.Entities;

namespace TeamNotificationService.Application.Interfaces.Repositories;

public interface INotificationRecipientRepository : IGenericRepository<NotificationRecipient>
{
    Task<IReadOnlyList<NotificationRecipient>> FindForUserAndSourceAppAsync(
        string userIdentitySubject,
        string sourceApp,
        IReadOnlyCollection<Guid>? notificationIds = null,
        bool unreadOnly = false,
        bool trackChanges = false,
        CancellationToken cancellationToken = default);
}
