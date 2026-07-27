namespace Marketify.CatalogImport;

public sealed record RichHouseCategorySeedDefinition(
    string Name,
    string Slug,
    string Description,
    int DisplayOrder,
    string? SeoTitle = null,
    string? SeoDescription = null);

public sealed record RichHouseVariantSeedDefinition(
    string SizeName,
    int StockQuantity,
    decimal? PriceOverride = null);

public sealed record RichHouseProductSeedDefinition(
    string ImportKey,
    string Name,
    string Slug,
    string Sku,
    string CategorySlug,
    string ShortDescription,
    string Description,
    decimal Price,
    decimal? CompareAtPrice,
    decimal? OfferPrice,
    DateTimeOffset? OfferStart,
    DateTimeOffset? OfferEnd,
    bool IsFeatured,
    bool IsBestSeller,
    bool IsNewArrival,
    string ColorName,
    string ColorSlug,
    IReadOnlyList<RichHouseVariantSeedDefinition> Variants,
    IReadOnlyList<string> SourceFiles);

public sealed record ImportedProductReportItem(
    string Name,
    string Category,
    string Color,
    int ImageCount,
    decimal Price);

public sealed record DuplicateAssetGroup(
    string Hash,
    IReadOnlyList<string> Files);

public sealed class RichHouseCatalogImportSummary
{
    public int SourceFilesFound { get; set; }
    public int ProductsImported { get; set; }
    public int CategoriesActivated { get; set; }
    public int AssignedImages { get; set; }
    public int OptimizedImagesGenerated { get; set; }
    public int ThumbnailsGenerated { get; set; }
    public int CorruptedFilesDetected { get; set; }
    public List<ImportedProductReportItem> Products { get; } = [];
    public List<DuplicateAssetGroup> DuplicateGroups { get; } = [];
    public List<string> CorruptedFiles { get; } = [];
    public List<string> UnassignedFiles { get; } = [];
}
