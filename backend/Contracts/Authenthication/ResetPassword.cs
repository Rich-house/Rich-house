namespace Marketify.Contracts.Authenthication
{
    public record ResetPassword
        (
        
        string Email,
        string Token,
        string NewPassword,
        string ConfirmPassword


        );

}
