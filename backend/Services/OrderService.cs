using Marketify.Contracts.Order;
using Marketify.Date;
using Marketify.Entites;
using Microsoft.EntityFrameworkCore;

namespace Marketify.Services
{
    public class OrderService(ApplicationDbContext dbContext) : IOrderService
    {
        private readonly ApplicationDbContext _context = dbContext;

        public async  Task<int> CreateOrderAsync(string userId, CheckoutRequest request)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

         
                var cart = await _context.Carts
                    .Include(c => c.Items)
                    .ThenInclude(i => i.Product)
                    .FirstOrDefaultAsync(c => c.UserId == userId);

                if (cart == null || !cart.Items.Any())
                    throw new Exception("Cart Empty");

                var order = new Order
                {
                    UserId = userId,
                    OrderDate = DateTime.UtcNow,
                    ShippingAddress = request.ShippingAddress,
                    City = request.City,
                    PhoneNumber = request.Phone,
                    Status = StaticsFile.pending,
                    TotalAmount = cart.TotalPrice,
                    OrderItems = new List<OrderItem>()
                };

                foreach (var cartItem in cart.Items)
                {
                    if (cartItem.Product!.StockQuantity < cartItem.Quantity)
                        throw new Exception($"Prouct {cartItem.Product.Name} NotValid At Now");

                    cartItem.Product.StockQuantity -= cartItem.Quantity;

                    order.OrderItems.Add(new OrderItem
                    {
                        ProductId = cartItem.ProductId,
                        Quantity = cartItem.Quantity,
                        PriceAtPurchase = cartItem.Price,
                        SelectedSize = cartItem.SelectedSize
                    });
                }

                _context.Orders.Add(order);
                _context.CartItems.RemoveRange(cart.Items); 

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return order.Id;
            
        }
       

        public async Task<OrderResponse?> GetOrderByIdAsync(int orderId, string userId)
        {
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId);

            if (order == null) return null;

            return new OrderResponse(
                order.Id,
                order.OrderDate,
                order.TotalAmount,
                order.Status.ToString(),
                order.ShippingAddress!,
                order.OrderItems.Select(oi => new OrderItemResponse(
                    oi.ProductId,
                    oi.Product!.Name,
                    oi.Product.Images.FirstOrDefault()!.ImageUrl ?? "",
                    oi.Quantity,
                    oi.PriceAtPurchase,
                    oi.Quantity * oi.PriceAtPurchase,
                    oi.SelectedSize
                )).ToList()
            );
        }
        

        public async Task<List<OrderResponse>> GetUserOrdersAsync(string userId)
        {
            return await _context.Orders
                .Where(o => o.UserId == userId)
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .OrderByDescending(o => o.OrderDate)
                .Select(o => new OrderResponse(
                    o.Id,
                    o.OrderDate,
                    o.TotalAmount,
                    o.Status.ToString(), 
                    o.ShippingAddress!,
                    o.OrderItems.Select(oi => new OrderItemResponse(
                        oi.ProductId,
                        oi.Product!.Name,
                        oi.Product.Images.FirstOrDefault()!.ImageUrl ?? "",
                        oi.Quantity,
                        oi.PriceAtPurchase,
                        oi.Quantity * oi.PriceAtPurchase,
                        oi.SelectedSize
                    )).ToList()
                )).ToListAsync();
        }
    }
}
