namespace Marketify.Contracts.OrderItem
{
    public record OrderItemRead
 (

        int ProductId,
    string ProductName,
    int Quantity,
    decimal PriceAtPurchase,
    string SelectedSize

 );

}
