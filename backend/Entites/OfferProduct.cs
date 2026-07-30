namespace Marketify.Entites;

public class OfferProduct
{
    public int OfferId { get; set; }
    public Offer Offer { get; set; } = null!;
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;
}
