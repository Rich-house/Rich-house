using Marketify.Entites;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Marketify.Date.EntitiesConfigurations
{
    public class ProductImageConfigurations : IEntityTypeConfiguration<ProductImage>
    {
        public void Configure(EntityTypeBuilder<ProductImage> builder)
        {
            builder.Property(image => image.ImageUrl).HasMaxLength(500);
            builder.Property(image => image.AltText).HasMaxLength(200);
            builder.Property(image => image.OriginalFileName).HasMaxLength(260);
            builder.Property(image => image.SourceHash).HasMaxLength(128);
            builder.Property(image => image.SortOrder).HasDefaultValue(0);

            builder.HasIndex(image => new { image.ProductId, image.SourceHash })
                .IsUnique()
                .HasFilter("[SourceHash] IS NOT NULL");

            builder.HasIndex(image => new { image.ProductId, image.SortOrder });

            builder.HasQueryFilter(x => x.Product != null && !x.Product.Category.IsDeleted);
        }
    }
}
