
using Marketify.Contracts.Order;
using Marketify.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Marketify.Controllers
{
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [Route("api/[controller]")]
    [ApiController]
    public class OrderController(IOrderService orderService) : ControllerBase
    {
        private readonly IOrderService _orderService = orderService;

        [HttpPost("checkout")]
        public async Task<IActionResult> Checkout(CheckoutRequest request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("uid");

            try
            {
                var orderId = await _orderService.CreateOrderAsync(userId!, request);
                return Ok(new { Message = "Order created successfully", OrderId = orderId });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpGet("my-orders")]
        public async Task<IActionResult> GetMyOrders()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("uid");
            var orders = await _orderService.GetUserOrdersAsync(userId!);
            return Ok(orders);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetOrderDetails(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("uid");
            var order = await _orderService.GetOrderByIdAsync(id, userId!);

            if (order == null) return NotFound("Order not found.");

            return Ok(order);
        }
    }
}
