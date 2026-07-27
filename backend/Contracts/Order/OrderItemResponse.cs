namespace Marketify.Contracts.Order
{
    public record OrderItemResponse
    (
    int ProductId,
    string ProductName,
    string ProductImage, 
    int Quantity,
    decimal PriceAtPurchase, 
    decimal TotalItemPrice,  
    string SelectedSize

    );
    
}
