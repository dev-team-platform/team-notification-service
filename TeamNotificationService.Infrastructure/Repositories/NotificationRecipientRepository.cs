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
}
