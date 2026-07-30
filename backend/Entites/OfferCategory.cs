namespace Marketify.Entites;

public class OfferCategory
{
    public int OfferId { get; set; }
    public Offer Offer { get; set; } = null!;
    public int CategoryId { get; set; }
    public Category Category { get; set; } = null!;
}
