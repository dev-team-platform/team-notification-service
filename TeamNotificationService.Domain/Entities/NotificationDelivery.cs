namespace TeamNotificationService.Domain.Entities;

public class NotificationDelivery
{
    public Guid Id { get; set; }
    public Guid NotificationRecipientId { get; set; }
    public string Channel { get; set; } = null!;
    public string Status { get; set; } = null!;
    public string? Destination { get; set; }
    public string? TemplateKey { get; set; }
    public DateTimeOffset? SentAt { get; set; }
    public DateTimeOffset? DeliveredAt { get; set; }
    public DateTimeOffset? FailedAt { get; set; }
    public int RetryCount { get; set; }
    public string? LastError { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public NotificationRecipient? NotificationRecipient { get; set; }
}
