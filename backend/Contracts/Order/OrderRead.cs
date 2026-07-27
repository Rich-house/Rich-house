using Marketify.Contracts.OrderItem;

namespace Marketify.Contracts.Order
{
    public record OrderRead
    (

        int Id,
    DateTime OrderDate,
    decimal TotalAmount,
    string Status,
    List<OrderItemRead> OrderItems

        );
}
