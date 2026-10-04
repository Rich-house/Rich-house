using Marketify.Contracts.Common;

namespace Marketify.Contracts.Product;

public sealed class AdminProductQuery
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 12;
    public string? Search { get; set; }
    public int? CategoryId { get; set; }
    public bool? IsActive { get; set; }
    public bool? LowStockOnly { get; set; }
}

public sealed record AdminProductListItemDto(
    int Id,
    string Name,
    string? Slug,
    int CategoryId,
    string CategoryName,
    string? MainImageUrl,
    decimal Price,
    decimal? CompareAtPrice,
    decimal? SalePrice,
    int StockQuantity,
    string Status,
    bool IsActive,
    bool IsFeatured,
    bool IsBestSeller,
    bool IsNewArrival);

public sealed record AdminProductImageDto(
    int? Id,
    string Url,
    bool IsMain,
    int SortOrder);

public sealed record AdminProductDetailsDto(
    int Id,
    string Name,
    string Slug,
    string? ShortDescription,
    string Description,
    int CategoryId,
    decimal Price,
    decimal? CompareAtPrice,
    decimal? OfferPrice,
    DateTimeOffset? OfferStart,
    DateTimeOffset? OfferEnd,
    int StockQuantity,
    string Status,
    bool IsActive,
    bool IsFeatured,
    bool IsBestSeller,
    bool IsNewArrival,
    IReadOnlyList<int> SelectedSizeIds,
    IReadOnlyList<string> Colors,
    IReadOnlyList<AdminProductImageDto> Images);

public sealed class UpsertProductDto
{
    public string Name { get; set; } = string.Empty;
    public string? Slug { get; set; }
    public string? ShortDescription { get; set; }
    public string Description { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public decimal Price { get; set; }
    public decimal? CompareAtPrice { get; set; }
    public decimal? OfferPrice { get; set; }
    public DateTimeOffset? OfferStart { get; set; }
    public DateTimeOffset? OfferEnd { get; set; }
    public int StockQuantity { get; set; }
    public string Status { get; set; } = "Draft";
    public bool IsActive { get; set; } = true;
    public bool IsFeatured { get; set; }
    public bool IsBestSeller { get; set; }
    public bool IsNewArrival { get; set; }
    public List<int> SelectedSizeIds { get; set; } = [];
    public List<string> Colors { get; set; } = [];
    public List<string> ImageOrder { get; set; } = [];
    public string? MainImageReference { get; set; }
    public List<IFormFile> Images { get; set; } = [];
    public bool FailOnDuplicateNameOrSlug { get; set; }
}
