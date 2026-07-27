using Marketify.Contracts.Cart;
using Marketify.Contracts.CartItem;
using Marketify.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Marketify.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CartController(ICartServices cartServices) : ControllerBase
    {
        private readonly ICartServices _cartServices = cartServices;

        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [HttpGet("GetCart")]
        public async Task<IActionResult> GetCart()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier); 
            var cart = await _cartServices.GetCartAsync(userId!);
            return Ok(cart);
        }

        [HttpPost("Add-To-Cart")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]

        public async Task<IActionResult> AddToCart(CartItemRequest request)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;


            var result = await _cartServices.AddToCartAsync(userId!, request);
            return result ? Ok(new { message = "Product added to cart" }) : BadRequest(new { message = "Failed to add product" });

        }
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [HttpDelete("removeItem/{productId}")]
        public async Task<IActionResult> RemoveFromCart(int productId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var result = await _cartServices.RemoveFromCartAsync(userId!, productId);
            return result ? Ok(new { message = true }) : NotFound();
        }


        [HttpPut("update-quantity")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]

        public async Task<IActionResult> UpdateQuantity([FromBody] UpdateQuantityDto dto)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (userId == null)
                return Unauthorized();

            var result = await _cartServices.UpdateQuantityAsync(userId, dto.ProductId, dto.NewQuantity);

            if (!result)
                return NotFound(new { message = "Item not found in cart" });

            return Ok(new { message = "Quantity updated successfully" });
        }
    }
}
