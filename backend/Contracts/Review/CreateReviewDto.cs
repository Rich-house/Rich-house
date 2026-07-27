namespace Marketify.Contracts.Review
{
    public class CreateReviewDto
    {
        public int Rating { get; set; } 
        public string Comment { get; set; } = string.Empty;
        public int ProductId { get; set; }
    }
}
