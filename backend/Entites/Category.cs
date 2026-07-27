namespace Marketify.Entites
{
    public class Category
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Slug { get; set; }
        public string? Description { get; set; }
        public string? ImageUrl { get; set; }
        public int DisplayOrder { get; set; }
        public bool IsActive { get; set; } = true;
        public string? SeoTitle { get; set; }
        public string? SeoDescription { get; set; }
        public bool IsDeleted { get; set; }
        public ICollection<Product> Products { get; set; } = new List<Product>();
    }
}
    
