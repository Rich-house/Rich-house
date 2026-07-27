namespace Marketify.Contracts.Cart
{
    public class UpdateQuantityDto
    {
        public int ProductId { get; set; }
        public int NewQuantity { get; set; }
    }
}
