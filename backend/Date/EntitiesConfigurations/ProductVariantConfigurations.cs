using Marketify.Entites;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Marketify.Date.EntitiesConfigurations
{
    public class ProductVariantConfigurations : IEntityTypeConfiguration<ProductVariant>
    {
        public void Configure(EntityTypeBuilder<ProductVariant> builder)
        {
            builder.Property(variant => variant.OptionKey)
                .IsRequired()
                .HasMaxLength(160);
            builder.Property(variant => variant.PriceOverride).HasColumnType("decimal(18,2)");
            builder.Property(variant => variant.ColorName).HasMaxLength(60);
            builder.Property(variant => variant.ColorSlug).HasMaxLength(80);
            builder.Property(variant => variant.Sku).HasMaxLength(64);
            builder.Property(variant => variant.Barcode).HasMaxLength(64);
            builder.Property(variant => variant.IsActive).HasDefaultValue(true);
            builder.Property(variant => variant.SortOrder).HasDefaultValue(0);

            builder.HasOne(variant => variant.Product)
                .WithMany(product => product.Variants)
                .HasForeignKey(variant => variant.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(variant => variant.Size)
                .WithMany()
                .HasForeignKey(variant => variant.SizeId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasIndex(variant => variant.Sku)
                .IsUnique()
                .HasFilter("[Sku] IS NOT NULL");

            builder.HasIndex(variant => new { variant.ProductId, variant.OptionKey })
                .IsUnique();

            builder.HasIndex(variant => new { variant.ProductId, variant.IsActive });

            builder.HasQueryFilter(variant => variant.Product != null && !variant.Product.Category.IsDeleted);
        }
    }
}
