using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using TeamNotificationService.Application.Interfaces.Contexts;

namespace TeamNotificationService.Infrastructure.Contexts;

public sealed class CurrentUserContext : ICurrentUserContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserContext(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string IdentitySubject =>
        _httpContextAccessor.HttpContext?.User.FindFirstValue("sub")
        ?? throw new UnauthorizedAccessException("User is not authenticated.");
}
