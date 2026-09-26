namespace TeamNotificationService.Application.Interfaces.Services;

public interface IHealthCheckService
{
    Task<bool> HealthCheckAsync(CancellationToken cancellationToken = default);
}