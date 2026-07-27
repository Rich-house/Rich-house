using Marketify.Entites;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Marketify.Date.EntitiesConfigurations
{
    public class OrderItemConfigurations : IEntityTypeConfiguration<OrderItem>
    {
        public void Configure(EntityTypeBuilder<OrderItem> builder)
        {
            builder.Property(x => x.PriceAtPurchase).HasColumnType("decimal(18,2)");
            builder.HasQueryFilter(x => x.Product != null && !x.Product.Category.IsDeleted);
        }
    }
}
