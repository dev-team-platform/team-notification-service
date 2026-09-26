using TeamNotificationService.Application.Interfaces.Repositories;
using TeamNotificationService.Application.Interfaces.Services;

namespace TeamNotificationService.Application.Services;

public class HealthCheckService : IHealthCheckService
{
    private readonly IHealthCheckRepository _healthCheckRepository;

    public HealthCheckService(IHealthCheckRepository healthCheckRepository)
    {
        _healthCheckRepository = healthCheckRepository;
    }

    public async Task<bool> HealthCheckAsync(CancellationToken cancellationToken = default)
    {
        return await _healthCheckRepository.HealthCheckAsync(cancellationToken);
    }
}