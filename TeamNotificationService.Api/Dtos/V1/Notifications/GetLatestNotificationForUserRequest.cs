using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using TeamNotificationService.Application.Models.Notifications;

namespace TeamNotificationService.Api.Dtos.V1.Notifications;

public sealed class GetLatestNotificationForUserRequest
{
    [Required]
    public string SourceApp { get; set; } = null!;

    public bool IsUnreadOnly { get; set; }

    public static GetLatestNotificationForUserRequestModel ToModel(GetLatestNotificationForUserRequest request)
    {
        return new GetLatestNotificationForUserRequestModel
        {
            SourceApp = request.SourceApp,
            IsUnreadOnly = request.IsUnreadOnly
        };
    }
}
