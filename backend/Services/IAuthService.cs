using Marketify.Abstraction;
using Marketify.Contracts.Authenthication;

namespace Marketify.Services
{
    public interface IAuthService
    {
        Task<Result<AuthResponse>> GetTokenAsync(string Email, string password, CancellationToken cancellationToken =default);
        Task<string>RegisterAsync(RegisterRequestUser model , CancellationToken cancellationToken = default);

        Task<string> ConfirmEmailAsync(ConfirmEmailRequest model);
        Task<string> ForgotPasswordAsync(ForgotPassword email);
        Task<string> ResetPasswordAsync(ResetPassword request);
        Task<GetUserInfo?> GetUserInfoAsync(string userId);
        Task<IEnumerable<DisplayAllUsers>> GetAllUsers();
        Task<GetUserByEmail?>GetUserByEmail(string email);
        Task<bool> DeleteUserByEmailAsync(string email);
    }
}
