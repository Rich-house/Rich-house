using Marketify.Entites;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Reflection.Emit;

namespace Marketify.Date.EntitiesConfigurations
{
    public class UserConfigurations : IEntityTypeConfiguration<ApplicationUser>
    {
        public void Configure(EntityTypeBuilder<ApplicationUser> builder)
        {
            builder.Property(x=>x.FirstName).HasMaxLength(70);
            builder.Property(x=>x.LastName).HasMaxLength(70);
            builder.Property(x=>x.storeName).HasMaxLength(60);
            builder.Property(x=>x.storeDescriptions).HasMaxLength(250);
            //builder.OwnsMany(x => x.refreshTokens)
            //    .ToTable("RefreshTokens")
            //    .WithOwner().HasForeignKey("UserId");

        }
    }
}
 