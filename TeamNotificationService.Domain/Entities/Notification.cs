using System.Text.Json.Nodes;

namespace TeamNotificationService.Domain.Entities;

public class Notification
{
    public Guid Id { get; set; }
    public string SourceApp { get; set; } = null!;
    public string EventType { get; set; } = null!;
    public string? Title { get; set; }
    public string? Content { get; set; }
    public JsonNode? Data { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}