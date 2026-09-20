using System.Text.Json;

namespace TeamNotificationService.Domain.Entities;

public class Notification
{
    public Guid Id { get; set; }
    public string? Title { get; set; }
    public string? Content { get; set; }
    public JsonDocument? Data { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public Guid CreatedById { get; set; }
}
