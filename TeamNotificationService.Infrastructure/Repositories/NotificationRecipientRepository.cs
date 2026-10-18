using Microsoft.EntityFrameworkCore;
using TeamNotificationService.Application.Interfaces.Repositories;
using TeamNotificationService.Domain.Entities;
using TeamNotificationService.Infrastructure.Persistence;

namespace TeamNotificationService.Infrastructure.Repositories;

public class NotificationRecipientRepository : GenericRepository<NotificationRecipient>, INotificationRecipientRepository
{
    public NotificationRecipientRepository(Serilog.ILogger logger, AppDbContext dbContext)
        : base(logger, dbContext)
    {
    }

    public async Task<IReadOnlyList<NotificationRecipient>> FindForUserAndSourceAppAsync(
        string userIdentitySubject,
        string sourceApp,
        IReadOnlyCollection<Guid>? notificationIds = null,
        bool unreadOnly = false,
        bool trackChanges = false,
        CancellationToken cancellationToken = default)
    {
        var query = from recipient in _dbContext.Set<NotificationRecipient>()
                    join notification in _dbContext.Set<Notification>()
                        on recipient.NotificationId equals notification.Id
                    where recipient.UserIdentitySubject == userIdentitySubject
                          && notification.SourceApp == sourceApp
                    select recipient;

        if (notificationIds is not null)
        {
            query = query.Where(recipient => notificationIds.Contains(recipient.NotificationId));
        }

        if (unreadOnly)
        {
            query = query.Where(recipient => !recipient.IsRead);
        }

        if (!trackChanges)
        {
            query = query.AsNoTracking();
        }

        return await query.ToListAsync(cancellationToken);
    }
}
