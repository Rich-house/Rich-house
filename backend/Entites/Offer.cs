namespace Marketify.Entites;

public enum OfferDiscountType
{
    Percentage = 1,
    FixedAmount = 2
}

public class Offer
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public OfferDiscountType DiscountType { get; set; } = OfferDiscountType.Percentage;
    public decimal DiscountValue { get; set; }
    public DateTimeOffset StartDate { get; set; }
    public DateTimeOffset EndDate { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public ICollection<OfferProduct> Products { get; set; } = new List<OfferProduct>();
    public ICollection<OfferCategory> Categories { get; set; } = new List<OfferCategory>();
}
