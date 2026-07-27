namespace Marketify.Contracts.Product
{
    public class ProductReadDto
    {
        public int Id { get; set; }
        public int CategoryId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Slug { get; set; }
        public string? ShortDescription { get; set; }
        public string Description { get; set; } = string.Empty;
        public int StockQuantity { get; set; }
        public decimal Price { get; set; }
        public decimal? CompareAtPrice { get; set; }
        public decimal? OfferPrice { get; set; }
        public DateTimeOffset? OfferStart { get; set; }
        public DateTimeOffset? OfferEnd { get; set; }
        public string Status { get; set; } = string.Empty;
        public bool IsFeatured { get; set; }
        public bool IsBestSeller { get; set; }
        public bool IsNewArrival { get; set; }
        public bool IsOnOffer { get; set; }
        public string? CategoryName { get; set; }
        public string? CategorySlug { get; set; }

        public List<string> ImageUrls { get; set; } = new();

        public List<string> Sizes { get; set; } = new();
    }
}
