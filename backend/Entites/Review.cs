namespace Marketify.Entites
{
    public class Review
    {
        public int Id { get; set; }

        public int Rating { get; set; }

        public string Comment { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public int ProductId { get; set; }
        public Product? Product { get; set; }

        public string? UserId { get; set; }
        public ApplicationUser? User { get; set; }
    }
}
