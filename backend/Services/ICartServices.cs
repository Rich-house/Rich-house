using Marketify.Contracts.Cart;
using Marketify.Contracts.CartItem;

namespace Marketify.Services
{
    public interface ICartServices
    {
        Task<CartResponse> GetCartAsync(string userId);
        Task<bool> AddToCartAsync(string userId, CartItemRequest dto);
        Task<bool> RemoveFromCartAsync(string userId, int productId);
        Task<bool> UpdateQuantityAsync(string userId, int productId, int newQuantity);
        Task<bool> ClearCartAsync(string userId);

    }
}
