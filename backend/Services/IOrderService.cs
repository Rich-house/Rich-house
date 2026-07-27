using Marketify.Contracts.Order;
using Marketify.Entites;

namespace Marketify.Services
{
    public interface IOrderService
    {
        Task<int> CreateOrderAsync(string userId, CheckoutRequest request);

        Task<List<OrderResponse>> GetUserOrdersAsync(string userId);

        Task<OrderResponse?> GetOrderByIdAsync(int orderId, string userId);
    }
}
