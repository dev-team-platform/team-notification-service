namespace TeamNotificationService.Api.Dtos.Common;

public class SortFieldRequest
{
    public string FieldName { get; init; } = null!;
    public bool IsAscending { get; init; }
}