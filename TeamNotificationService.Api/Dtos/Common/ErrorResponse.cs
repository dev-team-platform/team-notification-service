namespace TeamNotificationService.Api.Dtos.Common;

public class ErrorResponse
{
    public string Message { get; set; } = null!;
    public Dictionary<string, object?>? Details { get; set; }
}