using Marketify.Entites;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Marketify.Date.EntitiesConfigurations
{
    public class CartItemConfigurations : IEntityTypeConfiguration<CartItem>
    {
        public void Configure(EntityTypeBuilder<CartItem> builder)
        {
            builder.Property(x => x.Price).HasColumnType("decimal(18,2)");
            builder.HasQueryFilter(x => x.Product != null && !x.Product.Category.IsDeleted);
        }
    }
}
