using Marketify.Entites;

namespace Marketify.Authentication
{
    public interface IJwtProvider
    {
        (string token , int expiresIn)GenerateToken(ApplicationUser user, IList<string> roles);
        string? ValidateToken(string token);
    }
}
