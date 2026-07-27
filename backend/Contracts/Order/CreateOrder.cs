using Marketify.Contracts.OrderItem;

namespace Marketify.Contracts.Order
{
    public record CreateOrder
    (List<CreateOrderItem> OrderItems);
    
}
