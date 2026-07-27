using Marketify.Abstraction;

namespace Marketify.Erros
{
    public static class UserErrors
    {
        public static readonly Error InvalidCredentials 
            = new Error("User.InvalidCredentials", "Invalid Email/Password");
    }
}
