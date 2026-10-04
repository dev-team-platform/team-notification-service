using TeamNotificationService.Domain.Entities;
using TeamNotificationService.Domain.Utils;

namespace TeamNotificationService.Application.Models.NotificationDeliveries;

public class UpsertNotifcationDeliveryResponseModel
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

    public static UpsertNotifcationDeliveryResponseModel FromEntity(NotificationDelivery entity)
    {
        return new UpsertNotifcationDeliveryResponseModel
        {
            Id = entity.Id,
            NotificationRecipientId = entity.NotificationRecipientId,
            Channel = EnumUtils.ToString(entity.Channel),
            Status = EnumUtils.ToString(entity.Status),
            Destination = entity.Destination,
            TemplateKey = entity.TemplateKey,
            SentAt = entity.SentAt,
            DeliveredAt = entity.DeliveredAt,
            FailedAt = entity.FailedAt,
            RetryCount = entity.RetryCount,
            LastError = entity.LastError,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt
        };
    }
}