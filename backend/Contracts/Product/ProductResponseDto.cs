namespace Marketify.Contracts.Product
{
    public record ProductResponseDto(
        int Id,
        string Name,
        string Description,
        decimal Price,
        int StockQuantity,
        int CategoryId,
        string? CategoryName,
        string? CategorySlug,
        List<string> ImageUrls,
        List<string> Sizes,
        string? Slug,
        string? Sku,
        string? Barcode,
        string Status,
        string? ShortDescription,
        decimal? CompareAtPrice,
        decimal? OfferPrice,
        DateTimeOffset? OfferStart,
        DateTimeOffset? OfferEnd,
        bool IsActive,
        bool IsFeatured,
        bool IsBestSeller,
        bool IsNewArrival,
        string? SeoTitle,
        string? SeoDescription,
        List<ProductVariantDto> Variants
    );
}
