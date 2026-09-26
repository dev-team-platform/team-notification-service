using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using TeamNotificationService.Api.Constants;
using TeamNotificationService.Api.Dtos.Common;
using TeamNotificationService.Api.Dtos.V1.Notifications;
using TeamNotificationService.Application.Interfaces.Services.Notifications;

namespace TeamNotificationService.Api.V1.Controllers;

[Route("api/v{version:apiVersion}/notifications")]
[ApiVersion("1.0")]
[ApiController]
[Authorize]
[EnableRateLimiting(RateLimiterPolicies.Default)]
public class NotificationController : ControllerBase
{
    private readonly INotificationQueryService _notificationQueryService;
    private readonly INotificationCommandService _notificationCommandService;

    public NotificationController(
        INotificationQueryService notificationQueryService,
        INotificationCommandService notificationCommandService)
    {
        _notificationQueryService = notificationQueryService;
        _notificationCommandService = notificationCommandService;
    }

    [HttpGet("unread")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IReadOnlyList<GetUnreadNotificationsResponse>))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(ErrorResponse))]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ErrorResponse))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ErrorResponse))]
    [ProducesResponseType(StatusCodes.Status409Conflict, Type = typeof(ErrorResponse))]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity, Type = typeof(ErrorResponse))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ErrorResponse))]
    public async Task<IActionResult> GetUnreadNotificationsAsync(CancellationToken cancellationToken = default)
    {
        var result = await _notificationQueryService.GetUnreadNotificationsAsync(
            cancellationToken);

        var dtoResponse = result.Select(GetUnreadNotificationsResponse.FromModel).ToList();

        return Ok(dtoResponse);
    }

    [HttpPost("all")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(GetAllNotificationsResponse))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(ErrorResponse))]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ErrorResponse))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ErrorResponse))]
    [ProducesResponseType(StatusCodes.Status409Conflict, Type = typeof(ErrorResponse))]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity, Type = typeof(ErrorResponse))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ErrorResponse))]
    public async Task<IActionResult> GetAllNotificationsAsync(
        [FromBody] GetAllNotificationsRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await _notificationQueryService.GetAllNotificationsAsync(
            GetAllNotificationsRequest.ToModel(request),
            cancellationToken);

        var dtoResponse = GetAllNotificationsResponse.FromModel(result);

        return Ok(dtoResponse);
    }

    [HttpPatch("mark-as-read")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(MarkNotificationReadResponse))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(ErrorResponse))]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ErrorResponse))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ErrorResponse))]
    [ProducesResponseType(StatusCodes.Status409Conflict, Type = typeof(ErrorResponse))]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity, Type = typeof(ErrorResponse))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ErrorResponse))]
    public async Task<IActionResult> MarkNotificationReadAsync(
        [FromBody] MarkNotificationReadRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await _notificationCommandService.MarkNotificationReadAsync(
            MarkNotificationReadRequest.ToModel(request),
            cancellationToken);

        var dtoReponse = MarkNotificationReadResponse.FromModel(result);

        return Ok(dtoReponse);
    }
}
