using System.Security.Claims;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using TeamNotificationService.Application.Interfaces.Contexts;
using TeamNotificationService.Domain.Exceptions;

namespace TeamNotificationService.Infrastructure.Contexts;

public sealed class CurrentUserContext : ICurrentUserContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IHostEnvironment _hostEnvironment;

    public CurrentUserContext(IHttpContextAccessor httpContextAccessor, IHostEnvironment hostEnvironment)
    {
        _httpContextAccessor = httpContextAccessor;
        _hostEnvironment = hostEnvironment;
    }

    public string IdentitySubject
    {
        get
        {
            if (_hostEnvironment.IsDevelopment())
            {
                return "dev-user";
            }

            return _httpContextAccessor.HttpContext?.User.FindFirstValue("sub")
                ?? throw new UnauthorizedException("User is not authenticated.");
        }
    }
}
