using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using TeamNotificationService.Domain.Exceptions;

namespace TeamNotificationService.Api.Hubs;

[Authorize]
public sealed class NotificationHub : Hub<INotificationClient>
{
    public override async Task OnConnectedAsync()
    {
        var identitySubject = Context.User?.FindFirstValue("sub");

        if (string.IsNullOrWhiteSpace(identitySubject))
        {
            throw new UnauthorizedException("Authenticated hub connection is missing the 'sub' claim.");
        }

        await Groups.AddToGroupAsync(
            Context.ConnectionId,
            identitySubject,
            Context.ConnectionAborted);

        await base.OnConnectedAsync();
    }
}
