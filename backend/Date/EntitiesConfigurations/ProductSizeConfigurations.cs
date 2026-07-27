using Marketify.Entites;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Marketify.Date.EntitiesConfigurations
{
    public class ProductSizeConfigurations : IEntityTypeConfiguration<ProductSize>
    {
        public void Configure(EntityTypeBuilder<ProductSize> builder)
        {
            builder.HasOne(x => x.Product).WithMany(x => x.ProductSizes).HasForeignKey(x=>x.ProductId);
            builder.HasKey(x => new { x.ProductId, x.SizeId });
            builder.HasOne(ps => ps.Size)
                .WithMany()
                .HasForeignKey(ps => ps.SizeId);
            builder.HasQueryFilter(x => x.Product != null && !x.Product.Category.IsDeleted);

        }
    }
}
