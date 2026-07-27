namespace Marketify.Entites
{
    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Slug { get; set; }
        public string? Sku { get; set; }
        public string? Barcode { get; set; }
        public string Status { get; set; } = "Draft";
        public string? ShortDescription { get; set; }
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public decimal? CompareAtPrice { get; set; }
        public decimal? OfferPrice { get; set; }
        public DateTimeOffset? OfferStart { get; set; }
        public DateTimeOffset? OfferEnd { get; set; }
        public int StockQuantity { get; set; }
        public bool IsActive { get; set; } = true;
        public bool IsFeatured { get; set; }
        public bool IsBestSeller { get; set; }
        public bool IsNewArrival { get; set; }
        public string? SeoTitle { get; set; }
        public string? SeoDescription { get; set; }
        public string? ImportKey { get; set; }
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

        
        public int CategoryId { get; set; }
        public Category Category { get; set; } = null!;


        public ICollection<ProductImage> Images { get; set; } = new List<ProductImage>();
        public ICollection<ProductSize> ProductSizes { get; set; } = new List<ProductSize>();
        public ICollection<ProductVariant> Variants { get; set; } = new List<ProductVariant>();
        public ICollection<Review> Reviews { get; set; } = new List<Review>();
        public string? VendorId { get; set; }
        public ApplicationUser? Vendor { get; set; }
    }
}
