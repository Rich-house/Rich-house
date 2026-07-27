using Marketify.Entites;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Size = Marketify.Entites.Size;

namespace Marketify.Date.EntitiesConfigurations
{
    public class SizeConfigurations : IEntityTypeConfiguration<Size>
    {
        public void Configure(EntityTypeBuilder<Size> builder)
        {
            builder.Property(size => size.Name).HasMaxLength(30);

            builder.HasData(
              new Size { Id = 1, Name = "S", SortOrder = 10 },
              new Size { Id = 2, Name = "M", SortOrder = 20 },
              new Size { Id = 3, Name = "L", SortOrder = 30 },
              new Size { Id = 4, Name = "XL", SortOrder = 40 },
              new Size { Id = 5, Name = "XXL", SortOrder = 50 },
              new Size { Id = 20, Name = "46", SortOrder = 460 },
              new Size { Id = 21, Name = "48", SortOrder = 480 },
              new Size { Id = 22, Name = "50", SortOrder = 500 },
              new Size { Id = 23, Name = "52", SortOrder = 520 },
              new Size { Id = 24, Name = "54", SortOrder = 540 },
              new Size { Id = 25, Name = "56", SortOrder = 560 },
              new Size { Id = 26, Name = "58", SortOrder = 580 },
              new Size { Id = 27, Name = "60", SortOrder = 600 },
              new Size { Id = 40, Name = "40", SortOrder = 400 },
              new Size { Id = 41, Name = "41", SortOrder = 410 },
              new Size { Id = 42, Name = "42", SortOrder = 420 },
              new Size { Id = 43, Name = "43", SortOrder = 430 },
              new Size { Id = 44, Name = "44", SortOrder = 440 },
              new Size { Id = 45, Name = "45", SortOrder = 450 }
           );
        }
    }
}
