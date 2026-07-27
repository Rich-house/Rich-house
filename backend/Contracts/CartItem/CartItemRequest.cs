namespace Marketify.Contracts.CartItem
{
    public record CartItemRequest
        (
       int ProductId,
       int Quantity,
       string SelectedSize


        );
}
