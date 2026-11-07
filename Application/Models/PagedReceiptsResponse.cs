namespace Application.Models;

public sealed record PagedReceiptsResponse(
    IReadOnlyList<ReceiptsListItem> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages);
