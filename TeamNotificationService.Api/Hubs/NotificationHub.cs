using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using TeamNotificationService.Domain.Constants;
using TeamNotificationService.Domain.Exceptions;
using TeamNotificationService.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace TeamNotificationService.Api.Hubs;

[Authorize]
public sealed class NotificationHub : Hub<INotificationClient>
{
    private readonly SourceAppsOptions _sourceAppsOptions;

    public NotificationHub(IOptions<SourceAppsOptions> sourceAppsOptions)
    {
        _sourceAppsOptions = sourceAppsOptions.Value;
    }

    public override async Task OnConnectedAsync()
    {
        var identitySubject = Context.User?.FindFirstValue("sub");
        var sourceApp = Context.GetHttpContext()?.Request.Query["source_app"].ToString();

        if (string.IsNullOrWhiteSpace(identitySubject))
        {
            throw new UnauthorizedException("Authenticated hub connection is missing the 'sub' claim.");
        }

        if (!_sourceAppsOptions.IsAllowed(sourceApp))
        {
            throw new UnauthorizedException("Authenticated hub connection has an invalid 'source_app' value.");
        }

        await Groups.AddToGroupAsync(
            Context.ConnectionId,
            NotificationGroupName.ForUser(sourceApp!, identitySubject),
            Context.ConnectionAborted);

        await base.OnConnectedAsync();
    }
}
