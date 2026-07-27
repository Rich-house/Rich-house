namespace Marketify.Entites
{
    public class CartItem
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public Product? Product { get; set; }
        public decimal Price { get; set; }
        public decimal TotalItemPrice => Quantity * Price;
        public int Quantity { get; set; }
        public int CartId { get; set; }
        public Cart? Cart { get; set; }
        public string SelectedSize { get; set; } = string.Empty;
    }
}
