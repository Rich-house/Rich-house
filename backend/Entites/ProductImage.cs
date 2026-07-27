namespace Marketify.Entites
{
    public class ProductImage
    {
        public int Id { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
        public bool IsMain { get; set; }
        public string? AltText { get; set; }
        public int SortOrder { get; set; }
        public int? Width { get; set; }
        public int? Height { get; set; }
        public string? OriginalFileName { get; set; }
        public string? SourceHash { get; set; }

        public int ProductId { get; set; }
        public Product? Product { get; set; }
    }
}
