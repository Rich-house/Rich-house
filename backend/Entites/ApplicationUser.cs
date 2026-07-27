 using Microsoft.AspNetCore.Identity;

namespace Marketify.Entites
{
    public class ApplicationUser :IdentityUser
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string? Address { get; set; }
        public string?storeName { get; set; }
        public int PhoneNumberAddingKeyofCountry { get; set; }
        public string?   storeDescriptions { get; set; }
        public ICollection<Product>? OwnProducts { get; set; }



    }
}

        //public List<RefreshToken> refreshTokens { get; set; } = [];