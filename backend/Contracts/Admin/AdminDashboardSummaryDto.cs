namespace Marketify.Contracts.Admin;

public sealed record AdminDashboardSummaryDto(
    int TotalProducts,
    int ActiveProducts,
    int InactiveProducts,
    int TotalCategories,
    int ActiveOffers,
    int ActiveProductDiscounts,
    int LowStockProducts,
    IReadOnlyList<AdminRecentItemDto> RecentProducts,
    IReadOnlyList<AdminRecentItemDto> RecentOffers);

public sealed record AdminRecentItemDto(
    int Id,
    string Title,
    string? Subtitle,
    string? ImageUrl,
    DateTimeOffset CreatedAt,
    string Status);

public sealed record AdminLookupItemDto(int Id, string Name, string? Subtitle = null);
