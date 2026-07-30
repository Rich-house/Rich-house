using Marketify.Entites;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Marketify.Date.EntitiesConfigurations;

public class OfferCategoryConfigurations : IEntityTypeConfiguration<OfferCategory>
{
    public void Configure(EntityTypeBuilder<OfferCategory> builder)
    {
        builder.HasKey(link => new { link.OfferId, link.CategoryId });

        builder.HasOne(link => link.Offer)
            .WithMany(offer => offer.Categories)
            .HasForeignKey(link => link.OfferId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(link => link.Category)
            .WithMany()
            .HasForeignKey(link => link.CategoryId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasQueryFilter(link => !link.Category.IsDeleted);
    }
}
