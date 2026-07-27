namespace Marketify.Contracts.Authenthication
{
    public class JwtOptions
    {
        public string key { get; set; } = string.Empty;

        public string Issuer { get; set; } = string.Empty;
        public string Audience { get; set; } = string.Empty;
        public int ExpireyMinutes  { get; set; }
        public static string SectionName = "Jwt ";
    }
}
