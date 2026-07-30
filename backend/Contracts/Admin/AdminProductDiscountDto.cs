using Marketify.Contracts.Common;

namespace Marketify.Contracts.Admin;

public sealed class AdminProductDiscountQuery
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 12;
    public string? Search { get; set; }
    public string? Status { get; set; }
}

public sealed record AdminProductDiscountListItemDto(
    int Id,
    string Name,
    string? Slug,
    int CategoryId,
    string CategoryName,
    string? MainImageUrl,
    decimal Price,
    decimal? CompareAtPrice,
    decimal OfferPrice,
    DateTimeOffset? OfferStart,
    DateTimeOffset? OfferEnd,
    bool IsActive,
    bool IsCategoryActive,
    string Status);
