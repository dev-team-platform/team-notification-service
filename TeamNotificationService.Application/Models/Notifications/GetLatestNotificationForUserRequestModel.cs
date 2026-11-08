namespace TeamNotificationService.Application.Models.Notifications;

public sealed class GetLatestNotificationForUserRequestModel
{
    public string SourceApp { get; init; } = null!;
    public bool IsUnreadOnly { get; init; }
}
