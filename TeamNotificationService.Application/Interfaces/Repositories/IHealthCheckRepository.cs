namespace TeamNotificationService.Application.Interfaces.Repositories;

public interface IHealthCheckRepository
{
    Task<bool> HealthCheckAsync(CancellationToken cancellationToken = default);
}