namespace Marketify.Contracts.Order
{
    public record OrderResponse
    (
        int OrderId,
    DateTime OrderDate,
    decimal TotalPrice,
    string ShippingAddressm,
    string Status,
    List<OrderItemResponse> Items
        );
    
}
