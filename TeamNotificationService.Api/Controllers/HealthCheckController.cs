using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using TeamNotificationService.Api.Constants;
using TeamNotificationService.Application.Interfaces.Services;

namespace TeamNotificationService.Api.Controllers;

[Route("api/v{version:apiVersion}/notifications")]
[ApiVersion("1.0")]
[ApiController]
[EnableRateLimiting(RateLimiterPolicies.Default)]
public class HealthCheckController : ControllerBase
{
    private readonly IHealthCheckService _healthCheckService;

    public HealthCheckController(IHealthCheckService healthCheckService)
    {
        _healthCheckService = healthCheckService;
    }

    [HttpGet("app-health")]
    public IActionResult GetAppHealth()
    {
        return Ok(new { status = "Healthy" });
    }

    [HttpGet("db-health")]
    public async Task<IActionResult> GetDbHealth(CancellationToken cancellationToken = default)
    {
        var isHealthy = await _healthCheckService.HealthCheckAsync(cancellationToken);
        if (isHealthy)
        {
            return Ok(new { status = "Healthy" });
        }
        else
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { status = "Unhealthy" });
        }
    }
}