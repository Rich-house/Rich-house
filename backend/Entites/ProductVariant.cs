namespace Marketify.Entites
{
    public class ProductVariant
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public Product Product { get; set; } = null!;
        public string OptionKey { get; set; } = string.Empty;
        public int? SizeId { get; set; }
        public Size? Size { get; set; }
        public string? ColorName { get; set; }
        public string? ColorSlug { get; set; }
        public string? Sku { get; set; }
        public string? Barcode { get; set; }
        public decimal? PriceOverride { get; set; }
        public int StockQuantity { get; set; }
        public bool IsActive { get; set; } = true;
        public int SortOrder { get; set; }
    }
}
