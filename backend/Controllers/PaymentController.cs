using Marketify.PaymentServices;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Marketify.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PaymentController : ControllerBase
    {
        private readonly PaymobService _paymobService;
        private readonly IConfiguration _configuration;

        public PaymentController(PaymobService paymobService, IConfiguration configuration)
        {
            _paymobService = paymobService;
            _configuration = configuration;
        }

        [HttpPost("checkout")]
        public async Task<IActionResult> Checkout([FromBody] decimal amount)
        {
            var token = await _paymobService.GetAuthToken();
            if (string.IsNullOrEmpty(token)) return BadRequest("Fail to get Autrh Tooken");

            var orderId = await _paymobService.CreateOrder(token, amount);
            if (orderId == 0) return BadRequest("Faild to get auth token (Order ID is 0)");

            var paymentKey = await _paymobService.GetPaymentKey(token, orderId, amount);
            if (string.IsNullOrEmpty(paymentKey)) return BadRequest("Fail to gauin payment key(Payment Key is null)");

            var iframeId = _configuration["PaymobSettings:IframeId"];
            var paymentUrl = $"https://accept.paymob.com/api/acceptance/iframes/{iframeId}?payment_token={paymentKey}";

            return Ok(new { url = paymentUrl });
        }
    }
}
