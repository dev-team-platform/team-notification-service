using TeamNotificationService.Application.Interfaces.Repositories;
using TeamNotificationService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace TeamNotificationService.Infrastructure.Repositories;

public class HealthCheckRepository : IHealthCheckRepository
{
    private readonly AppDbContext _dbContext;

    public HealthCheckRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<bool> HealthCheckAsync(CancellationToken cancellationToken = default)
    {
        var connected = await _dbContext.Database.CanConnectAsync(cancellationToken);

        await _dbContext.Notifications.AsNoTracking().AnyAsync(cancellationToken);

        return connected;
    }
}