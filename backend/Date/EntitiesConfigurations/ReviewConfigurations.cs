using Marketify.Entites;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Marketify.Date.EntitiesConfigurations
{
    public class ReviewConfigurations : IEntityTypeConfiguration<Review>
    {
        public void Configure(EntityTypeBuilder<Review> builder)
        {
            builder.HasQueryFilter(x => x.Product != null && !x.Product.Category.IsDeleted);
        }
    }
}
