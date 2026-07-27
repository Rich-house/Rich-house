using System.Text.Json;

namespace Marketify.PaymentServices
{
    public class PaymobService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public PaymobService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient; 
            _configuration = configuration; 
        }

        public async Task<string?> GetAuthToken()
        {
            var url = "https://accept.paymob.com/api/auth/tokens";

            var data = new { api_key = _configuration["PaymobSettings:ApiKey"] };

            var response = await _httpClient.PostAsJsonAsync(url, data);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<JsonElement>();
                return result.GetProperty("token").GetString()!;
            }

            return null;
        }
        public async Task<int> CreateOrder(string token, decimal amount)
        {
            var url = "https://accept.paymob.com/api/ecommerce/orders";

            var data = new
            {
                auth_token = token,
                delivery_needed = false, 
                amount_cents = (int)(amount * 100),
                currency = "EGP",
                items = new List<object>()
            };

            var response = await _httpClient.PostAsJsonAsync(url, data);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<JsonElement>();
                return result.GetProperty("id").GetInt32();
            }

            return 0;
        }
        public async Task<string?> GetPaymentKey(string token, int orderId, decimal amount)
        {
            var url = "https://accept.paymob.com/api/acceptance/payment_keys";

            // ركز جداً في الجزء ده: بايموب بيحتاج القيم أرقام صريحة مش نصوص
            var data = new
            {
                auth_token = token,
                amount_cents = (int)(amount * 100),
                expiration = 3600,
                order_id = orderId, // Integer
                billing_data = new
                {
                    first_name = "Amr",
                    last_name = "Khaled",
                    email = "test@test.com",
                    phone_number = "+201154237429", // جرب بصيغة دولية
                    apartment = "8",
                    floor = "1",
                    street = "Shubra",
                    building = "10",
                    shipping_method = "PKG",
                    postal_code = "12345",
                    city = "Cairo",
                    country = "EG",
                    state = "Cairo"
                },
                currency = "EGP",
                // أهم سطر: تأكد أن الـ ID رقم وليس نص
                integration_id = Convert.ToInt32(_configuration["PaymobSettings:IntegrationId"])
            };

            var response = await _httpClient.PostAsJsonAsync(url, data);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<JsonElement>();
                return result.GetProperty("token").GetString();
            }

            // السطر ده هيخلينا نشوف "ليه" بايموب رفضت الطلب في الـ Output بتاع الـ Visual Studio
            var errorBody = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"Paymob Rejection Reason: {errorBody}");

            return null;
        }
    }
}
