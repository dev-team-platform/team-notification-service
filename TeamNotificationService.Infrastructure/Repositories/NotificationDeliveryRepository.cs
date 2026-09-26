using TeamNotificationService.Application.Interfaces.Repositories;
using TeamNotificationService.Domain.Entities;
using TeamNotificationService.Infrastructure.Persistence;

namespace TeamNotificationService.Infrastructure.Repositories;

public class NotificationDeliveryRepository : GenericRepository<NotificationDelivery>, INotificationDeliveryRepository
{
    public NotificationDeliveryRepository(Serilog.ILogger logger, AppDbContext dbContext)
        : base(logger, dbContext)
    {
    }
}
