using Microsoft.EntityFrameworkCore;
using System.Reflection.Metadata.Ecma335;

namespace Marketify.Entites
{
    [Owned]
    public class RefreshToken
    {
        public string Token { get; set; } = string.Empty;
        public DateTime ExpiresOn { get; set; }
        public DateTime CreatedIn { get; set; } = DateTime.UtcNow;
        public DateTime? EndedAt { get; set; } = DateTime.UtcNow;
        public bool IsExpired => DateTime.UtcNow >= ExpiresOn;
        public bool IsActive => EndedAt is null && !IsActive;
    }
}
