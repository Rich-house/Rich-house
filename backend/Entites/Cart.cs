namespace Marketify.Entites
{
    public class Cart
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public ApplicationUser? User { get; set; }
        public ICollection<CartItem> Items { get; set; } = new List<CartItem>();
        public decimal TotalPrice => Items.Sum(item => item.Quantity * item.Product!.Price);
    }
}
