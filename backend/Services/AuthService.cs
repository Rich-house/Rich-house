using Marketify.Abstraction;
using Marketify.Authentication;
using Marketify.Contracts.Authenthication;
using Marketify.Date;
using Marketify.Entites;
using Marketify.Erros;
using Marketify.Helper;
using Marketify.Roles;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;

namespace Marketify.Services
{
    public class AuthService(UserManager<ApplicationUser> usermanger, IJwtProvider jwtProvider,
        ILogger<AuthService> logger, IEmailSender emailSender, ApplicationDbContext dbContext) : IAuthService
    {
        private readonly UserManager<ApplicationUser> _userManger = usermanger;
        private readonly IJwtProvider _jwtProvider = jwtProvider;
        private readonly ILogger<AuthService> _logger = logger;
        private readonly IEmailSender _emailSender = emailSender;
        private readonly ApplicationDbContext _context = dbContext;

        public async Task<Result<AuthResponse>> GetTokenAsync(string Email, string password, CancellationToken cancellationToken = default)
        {
            var user = await _userManger.FindByEmailAsync(Email);

            if (user == null) return 
                    Result.Failure<AuthResponse>(UserErrors.InvalidCredentials);

            var isValid = await _userManger.CheckPasswordAsync(user, password);
            if (!isValid)
            {
                return Result.Failure<AuthResponse>(UserErrors.InvalidCredentials);
            }

            var userRoles = await _userManger.GetRolesAsync(user);
            var (token, expiresIn) = _jwtProvider.GenerateToken(user, userRoles);
            var time = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
            var emailBody = EmailBodyHelper.GenerateEmailBody("EmailUserLogIn",
                new Dictionary<string, string>
                {
            { "{{username}}", user.FirstName },
            { "{{login_time}}",time }
                }
            );

            try
            {
                await _emailSender.SendEmailAsync(user.Email!, "Marketify  : UserlogedIn ✅", emailBody);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Failed to send login notification email for user {UserId}. Continuing successful sign-in.",
                    user.Id);
            }

            var response = new AuthResponse(user.Id, user.Email, user.FirstName, user.LastName, token, expiresIn * 60);
            return Result.Success<AuthResponse>(response);
        }

        public async Task<string> RegisterAsync(RegisterRequestUser model, CancellationToken cancellationToken = default)
        {
            var userExists = await _userManger.FindByEmailAsync(model.Email);
            if (userExists != null)
            {
                return "User Already Exists";
            }

            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                FirstName = model.FirstName,
                LastName = model.LastName,
                Address = model.Address,
                PhoneNumber = model.PhoneNumber
            };

            var result = await _userManger.CreateAsync(user, model.Password);

            if (!result.Succeeded)
            {
                return result.Errors.FirstOrDefault()?.Description ?? "Registration Failed";
            }
            var roleResult = await _userManger.AddToRoleAsync(user, AppRoles.Customer);
            var code = await _userManger.GenerateTwoFactorTokenAsync(user, "Email");

            var emailBody = EmailBodyHelper.GenerateEmailBody("Emailconfirmation",
                new Dictionary<string, string>
                {
            { "{{name}}", user.FirstName },
            { "{{code}}",code }
                }
            );

            await _emailSender.SendEmailAsync(user.Email!, "Marketify  : EmailConfirmation ✅", emailBody);

            return string.Empty;
        }
        public async Task<string> ConfirmEmailAsync(ConfirmEmailRequest model)
        {
            var user = await _userManger.FindByEmailAsync(model.email);

            if (user == null)
                return "Invalid Email or User Not Found";

            if (user.EmailConfirmed)
                return "Email is already confirmed";

            var isValid = await _userManger.VerifyTwoFactorTokenAsync(user, "Email", model.code);

            if (!isValid)
            {
                return "Invalid or Expired verification code";
            }

            user.EmailConfirmed = true;
            var result = await _userManger.UpdateAsync(user);

            return result.Succeeded ? string.Empty : "An error occurred while confirming your email";

        }

        public async Task<string> ForgotPasswordAsync(ForgotPassword request)
        {
            var user = await _userManger.FindByEmailAsync(request.Email);
            string successMessage = "If your email exists, a 6-digit code has been sent to your inbox.";

            if (user == null)
                return successMessage;

            var code = await _userManger.GenerateTwoFactorTokenAsync(user, TokenOptions.DefaultEmailProvider);
            var values = new Dictionary<string, string>
    {
        { "{{name}}", user.FirstName ?? "User" },
        { "{{code}}", code }
    };

            var emailBody = EmailBodyHelper.GenerateEmailBody("ResetPassword", values);

            try
            {
                await _emailSender.SendEmailAsync(user.Email!, "Marketify : Reset Password Code ✅", emailBody);
            }
            catch
            {
                return "Error sending email. Please try again later.";
            }

            return successMessage;
        }

        public async Task<string> ResetPasswordAsync(ResetPassword request)
        {
            var user = await _userManger.FindByEmailAsync(request.Email);
            if (user == null)
                return "Invalid request";

            var isValid = await _userManger.VerifyTwoFactorTokenAsync(
                user,
                TokenOptions.DefaultEmailProvider,
                request.Token
            );

            if (!isValid)
                return "Invalid or expired code";

            var resetToken = await _userManger.GeneratePasswordResetTokenAsync(user);
            var result = await _userManger.ResetPasswordAsync(user, resetToken, request.NewPassword);

            if (!result.Succeeded)
                return string.Join(", ", result.Errors.Select(e => e.Description));

            return "Password has been reset successfully ✅";
        }

        public async Task<GetUserInfo?> GetUserInfoAsync(string userId)
        {
            var user = await _context.Users
        .FirstOrDefaultAsync(u => u.Id == userId);

            if (user is null)
                return null;

            return new GetUserInfo
            (
                user.Id,
                user.FirstName,
                user.LastName,
                user.Email!,
                user.PhoneNumber,
           user.Address
            );


        }
        public async Task<IEnumerable<DisplayAllUsers>> GetAllUsers()
        {
            var users = await _userManger.Users.ToListAsync();
            var userwithrole = new List<DisplayAllUsers>();

            foreach (var user in users)
            {
                var roles = await _userManger.GetRolesAsync(user);

                userwithrole.Add(new DisplayAllUsers
                {
                    DisplayName = $"{user.FirstName} {user.LastName}", 
                    Email = user.Email!,
                    Id = user.Id,
                    Roles = roles
                }); 
            }

            return userwithrole;
        }

        public async Task<GetUserByEmail?> GetUserByEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return null;
            }

            var user = await _userManger.FindByEmailAsync(email); 
            if (user == null)
            {
                return null;
            }

            var roles = await _userManger.GetRolesAsync(user);
            var userByEmail = new GetUserByEmail
                (
                    Id: user.Id,
                    FullName: $"{user.FirstName} {user.LastName}",
                    Email: email,
                    PhoneNumber: user.PhoneNumber,
                    address: user.Address,
                    Roles: roles
                );

            return userByEmail;
        }

        public async Task<bool> DeleteUserByEmailAsync(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            var user = await _userManger.FindByEmailAsync(email);

            if (user == null)
                return false;

            var result = await _userManger.DeleteAsync(user);

            return result.Succeeded;
        }
    }
}
