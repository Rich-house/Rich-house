namespace Marketify.Contracts.Common;

public sealed record PaginationMeta(
    int Page,
    int PageSize,
    int TotalItems,
    int TotalPages,
    bool HasPreviousPage,
    bool HasNextPage);

public sealed record PagedResult<T>(
    IReadOnlyList<T> Items,
    PaginationMeta Meta);
