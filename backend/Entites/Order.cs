namespace Marketify.Entites
{
    public class Order
    {
        public int Id { get; set; }
        public DateTime OrderDate { get; set; } = DateTime.UtcNow;
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = "Pending"; // Pending, Shipped, Delivered
        public string? ShippingAddress {  get; set; }    
        public string? PhoneNumber {  get; set; }
        public string? City { get; set; }
        public string? Street { get; set; }
        public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
        public string? UserId { get; set; }
        public ApplicationUser? User { get; set; }
    }
}
