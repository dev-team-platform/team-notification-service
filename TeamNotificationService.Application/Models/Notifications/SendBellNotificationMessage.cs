using System.Text.Json;

namespace TeamNotificationService.Application.Models.Notifications;

public sealed class SendBellNotificationMessage
{
    public Guid NotificationId { get; init; }
    public Guid RecipientId { get; set; }
    public string? Title { get; init; }
    public string? Content { get; init; }
    public JsonDocument? Data { get; init; }
    public DateTimeOffset SentAt { get; init; }
    public bool IsRead { get; init; }
    public DateTimeOffset? ReadAt { get; init; }

    public SendBellNotificationMessage Clone()
    {
        return new SendBellNotificationMessage
        {
            NotificationId = NotificationId,
            RecipientId = RecipientId,
            Title = Title,
            Content = Content,
            Data = Data,
            SentAt = SentAt,
            IsRead = IsRead,
            ReadAt = ReadAt,
        };
    }
}
