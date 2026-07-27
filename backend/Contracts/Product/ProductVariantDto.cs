namespace Marketify.Contracts.Product
{
    public record ProductVariantDto(
        int Id,
        string OptionKey,
        string? Sku,
        string? Barcode,
        string? ColorName,
        string? ColorSlug,
        int? SizeId,
        string? SizeName,
        int StockQuantity,
        decimal? PriceOverride,
        bool IsActive,
        int SortOrder);
}
