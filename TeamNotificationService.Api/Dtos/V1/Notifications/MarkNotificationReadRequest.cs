using System.ComponentModel.DataAnnotations;
using TeamNotificationService.Application.Models.Notifications;

namespace TeamNotificationService.Api.Dtos.V1.Notifications;

public sealed class MarkNotificationReadRequest
{
    [Required]
    public string SourceApp { get; init; } = null!;
    public IReadOnlyList<Guid>? NotificationIds { get; init; }
    public bool IsAll { get; init; }

    public static MarkNotificationReadRequestModel ToModel(MarkNotificationReadRequest request)
    {
        return new MarkNotificationReadRequestModel
        {
            SourceApp = request.SourceApp,
            NotificationIds = (request.NotificationIds ?? Array.Empty<Guid>())
                .Distinct()
                .ToArray(),
            IsAll = request.IsAll
        };
    }
}
