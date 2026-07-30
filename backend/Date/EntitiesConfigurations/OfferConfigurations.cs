using Marketify.Entites;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Marketify.Date.EntitiesConfigurations;

public class OfferConfigurations : IEntityTypeConfiguration<Offer>
{
    public void Configure(EntityTypeBuilder<Offer> builder)
    {
        builder.Property(offer => offer.Title)
            .IsRequired()
            .HasMaxLength(160);
        builder.Property(offer => offer.Description).HasMaxLength(1000);
        builder.Property(offer => offer.ImageUrl).HasMaxLength(500);
        builder.Property(offer => offer.DiscountValue).HasColumnType("decimal(18,2)");
        builder.Property(offer => offer.IsActive).HasDefaultValue(true);
        builder.Property(offer => offer.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        builder.Property(offer => offer.UpdatedAt).HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasIndex(offer => new { offer.IsActive, offer.StartDate, offer.EndDate });
    }
}
