namespace Marketify.Contracts.Authenthication
{
    public record GetUserByEmail
    (
    string Id,
    string FullName,
    string Email,
    string? PhoneNumber,
    string? address,
    IList<string> Roles








        );
    
}
