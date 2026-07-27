namespace Marketify.Contracts.Authenthication
{
    public record RegisterRequestUser
  ( 
         string Email,
        string Password,
        string FirstName,
        string LastName,
        string Address,
        string PhoneNumber



   );
}
