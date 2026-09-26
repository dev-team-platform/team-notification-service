using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using TeamNotificationService.Application.Interfaces.Contexts;

namespace TeamNotificationService.Api.Hubs;

[Authorize]
public sealed class NotificationHub : Hub<INotificationClient>
{
    private readonly ICurrentUserContext _currentUserContext;

    public NotificationHub(ICurrentUserContext currentUserContext)
    {
        _currentUserContext = currentUserContext;
    }

    public override async Task OnConnectedAsync()
    {
        await Groups.AddToGroupAsync(
            Context.ConnectionId,
            _currentUserContext.IdentitySubject,
            Context.ConnectionAborted);

        await base.OnConnectedAsync();
    }
}
