using Marketify.Contracts.Common;

namespace Marketify.Contracts.Offer;

public sealed class AdminOfferQuery
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 12;
    public string? Search { get; set; }
    public string? Status { get; set; }
}

public sealed record AdminOfferListItemDto(
    int Id,
    string Title,
    string? ImageUrl,
    string DiscountType,
    decimal DiscountValue,
    DateTimeOffset StartDate,
    DateTimeOffset EndDate,
    bool IsActive,
    int ProductCount,
    int CategoryCount,
    string Status);

public sealed record AdminOfferDetailsDto(
    int Id,
    string Title,
    string? Description,
    string? ImageUrl,
    string DiscountType,
    decimal DiscountValue,
    DateTimeOffset StartDate,
    DateTimeOffset EndDate,
    bool IsActive,
    IReadOnlyList<int> ProductIds,
    IReadOnlyList<int> CategoryIds);

public sealed class UpsertOfferDto
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string DiscountType { get; set; } = "Percentage";
    public decimal DiscountValue { get; set; }
    public DateTimeOffset StartDate { get; set; }
    public DateTimeOffset EndDate { get; set; }
    public bool IsActive { get; set; } = true;
    public bool RemoveImage { get; set; }
    public IFormFile? Image { get; set; }
    public List<int> ProductIds { get; set; } = [];
    public List<int> CategoryIds { get; set; } = [];
}
