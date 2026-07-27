namespace Marketify.Contracts.Authenthication
{
    public record ConfirmEmailRequest
    (
        string email,
        string code
     );
}
