namespace TeamNotificationService.Application.Models.Common;

public class PaginationResult<TModel> where TModel : class
{
    public IReadOnlyList<TModel> Items { get; init; } = [];
    public int CurrentPage { get; init; }
    public int ItemsPerPage { get; init; }
    public int TotalItems { get; init; }
    public int TotalPages { get; init; }
    public bool HasPreviousPage => CurrentPage > 1;
    public bool HasNextPage => CurrentPage < TotalPages;
}