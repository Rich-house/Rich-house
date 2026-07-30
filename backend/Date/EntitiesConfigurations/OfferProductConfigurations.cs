using Marketify.Entites;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Marketify.Date.EntitiesConfigurations;

public class OfferProductConfigurations : IEntityTypeConfiguration<OfferProduct>
{
    public void Configure(EntityTypeBuilder<OfferProduct> builder)
    {
        builder.HasKey(link => new { link.OfferId, link.ProductId });

        builder.HasOne(link => link.Offer)
            .WithMany(offer => offer.Products)
            .HasForeignKey(link => link.OfferId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(link => link.Product)
            .WithMany()
            .HasForeignKey(link => link.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasQueryFilter(link => link.Product != null && !link.Product.Category.IsDeleted);
    }
}
