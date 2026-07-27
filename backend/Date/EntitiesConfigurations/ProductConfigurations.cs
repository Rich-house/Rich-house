using Marketify.Entites;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Marketify.Date.EntitiesConfigurations
{
    public class ProductConfigurations : IEntityTypeConfiguration<Product>
    {
        public void Configure(EntityTypeBuilder<Product> builder)
        {
            builder.Property(p => p.Price).HasColumnType("decimal(18,2)");
            builder.Property(p => p.CompareAtPrice).HasColumnType("decimal(18,2)");
            builder.Property(p => p.OfferPrice).HasColumnType("decimal(18,2)");
            builder.Property(p => p.Name)
                .IsRequired()
                .HasMaxLength(200);
            builder.Property(p => p.Slug).HasMaxLength(220);
            builder.Property(p => p.Sku).HasMaxLength(64);
            builder.Property(p => p.Barcode).HasMaxLength(64);
            builder.Property(p => p.Status).HasMaxLength(24).HasDefaultValue("Draft");
            builder.Property(p => p.ShortDescription).HasMaxLength(500);
            builder.Property(p => p.Description).HasMaxLength(4000);
            builder.Property(p => p.SeoTitle).HasMaxLength(160);
            builder.Property(p => p.SeoDescription).HasMaxLength(320);
            builder.Property(p => p.ImportKey).HasMaxLength(120);
            builder.Property(p => p.IsActive).HasDefaultValue(true);
            builder.Property(p => p.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
            builder.Property(p => p.UpdatedAt).HasDefaultValueSql("SYSUTCDATETIME()");

            builder.HasIndex(p => p.Slug)
                .IsUnique()
                .HasFilter("[Slug] IS NOT NULL");

            builder.HasIndex(p => p.Sku)
                .IsUnique()
                .HasFilter("[Sku] IS NOT NULL");

            builder.HasIndex(p => p.ImportKey)
                .IsUnique()
                .HasFilter("[ImportKey] IS NOT NULL");

            builder.HasIndex(p => new { p.IsActive, p.CategoryId });
            builder.HasIndex(p => new { p.IsFeatured, p.IsActive });
            builder.HasIndex(p => new { p.IsBestSeller, p.IsActive });
            builder.HasIndex(p => new { p.IsNewArrival, p.IsActive });

            builder.HasQueryFilter(p => !p.Category.IsDeleted);
        }
    }
}
