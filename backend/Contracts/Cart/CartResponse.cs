using Marketify.Contracts.CartItem;

namespace Marketify.Contracts.Cart
{
    public record CartResponse
(
    int CartId,
    List<CartItemResponse> Items,
    decimal TotalPrice,
    int TotalCount


        );
}
