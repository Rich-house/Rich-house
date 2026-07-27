using Marketify.Entites;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Marketify.Date.EntitiesConfigurations
{
    public class CategoryConfigurations : IEntityTypeConfiguration<Category>
    {
        public void Configure(EntityTypeBuilder<Category> builder)
        {
            builder.Property(category => category.Name)
                .IsRequired()
                .HasMaxLength(120);
            builder.Property(category => category.Slug).HasMaxLength(140);
            builder.Property(category => category.Description).HasMaxLength(500);
            builder.Property(category => category.ImageUrl).HasMaxLength(500);
            builder.Property(category => category.SeoTitle).HasMaxLength(160);
            builder.Property(category => category.SeoDescription).HasMaxLength(320);
            builder.Property(category => category.IsActive).HasDefaultValue(true);

            builder.HasIndex(category => category.Slug)
                .IsUnique()
                .HasFilter("[Slug] IS NOT NULL");

            builder.HasIndex(category => new { category.IsActive, category.DisplayOrder });

            builder.HasData(
    new Category { Id = 1, Name = "Electronics", IsDeleted = false },
    new Category { Id = 2, Name = "Fashion", IsDeleted = false },
    new Category { Id = 3, Name = "Home & Kitchen", IsDeleted = false },
    new Category { Id = 4, Name = "Beauty & Personal Care", IsDeleted = false },
    new Category { Id = 5, Name = "Sports & Fitness", IsDeleted = false },
    new Category { Id = 6, Name = "Books", IsDeleted = false },
    new Category { Id = 7, Name = "Toys & Games", IsDeleted = false },
    new Category { Id = 8, Name = "Automotive", IsDeleted = false },
    new Category { Id = 9, Name = "Health", IsDeleted = false },
    new Category { Id = 10, Name = "Grocery", IsDeleted = false },
    new Category { Id = 11, Name = "Mobile Phones", IsDeleted = false },
    new Category { Id = 12, Name = "Laptops", IsDeleted = false },
    new Category { Id = 13, Name = "Cameras", IsDeleted = false },
    new Category { Id = 14, Name = "Headphones", IsDeleted = false },
    new Category { Id = 15, Name = "Men Clothing", IsDeleted = false },
    new Category { Id = 16, Name = "Women Clothing", IsDeleted = false },
    new Category { Id = 17, Name = "Kids Clothing", IsDeleted = false },
    new Category { Id = 18, Name = "Shoes", IsDeleted = false },
    new Category { Id = 19, Name = "Bags & Accessories", IsDeleted = false },
    new Category { Id = 20, Name = "Furniture", IsDeleted = false },
    new Category { Id = 21, Name = "Home Decor", IsDeleted = false },
    new Category { Id = 22, Name = "Kitchen Appliances", IsDeleted = false },
    new Category { Id = 23, Name = "Bedding", IsDeleted = false },
    new Category { Id = 24, Name = "Skincare", IsDeleted = false },
    new Category { Id = 25, Name = "Makeup", IsDeleted = false },
    new Category { Id = 26, Name = "Hair Care", IsDeleted = false },
    new Category { Id = 27, Name = "Fragrances", IsDeleted = false },
    new Category { Id = 28, Name = "Gaming Consoles", IsDeleted = false },
    new Category { Id = 29, Name = "Office Supplies", IsDeleted = false },
    new Category { Id = 30, Name = "Pet Supplies", IsDeleted = false }
);
            builder.HasQueryFilter(category => !category.IsDeleted);
        }
    }
}
