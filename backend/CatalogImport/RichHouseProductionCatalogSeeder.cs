using Marketify.Date;
using Marketify.Entites;
using Microsoft.EntityFrameworkCore;

namespace Marketify.CatalogImport;

public static class RichHouseProductionCatalogSeeder
{
    private const string BeltsCategoryName = "Belts";
    private const string BeltsCategorySlug = "belts";
    private const int BeltsDisplayOrder = 50;
    private static readonly int[] BeltSizeIds = [40, 42, 44];

    private static readonly BeltProductSeed[] BeltProducts =
    [
        new(
            Name: "Classic Black Leather Belt",
            Slug: "classic-black-leather-belt",
            ShortDescription: "A refined black leather belt with a polished metal buckle for formal and everyday styling.",
            Description: "Premium-look black leather belt designed to complement suits, trousers, and smart casual outfits.",
            Price: 890m,
            CompareAtPrice: 1090m,
            StockQuantity: 12,
            ImageUrl: "/catalog/products/classic-black-leather-belt/classic-black-leather-belt-main.webp",
            ImageWidth: 859,
            ImageHeight: 223,
            ImportKey: "rich-house-belts-classic-black-leather-belt"),
        new(
            Name: "Textured Black Formal Belt",
            Slug: "textured-black-formal-belt",
            ShortDescription: "A textured black formal belt with a sleek buckle and a clean contemporary finish.",
            Description: "A versatile formal belt suitable for businesswear, tailoring, and polished evening looks.",
            Price: 990m,
            CompareAtPrice: 1190m,
            StockQuantity: 10,
            ImageUrl: "/catalog/products/textured-black-formal-belt/textured-black-formal-belt-main.webp",
            ImageWidth: 599,
            ImageHeight: 699,
            ImportKey: "rich-house-belts-textured-black-formal-belt")
    ];

    public static async Task SeedBeltsCatalogAsync(
        ApplicationDbContext dbContext,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var category = await dbContext.Categories
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(
                item => item.Slug == BeltsCategorySlug || item.Name == BeltsCategoryName,
                cancellationToken);

        if (category is null)
        {
            category = new Category
            {
                Name = BeltsCategoryName,
                Slug = BeltsCategorySlug,
                Description = "Leather belts and dress-ready finishing pieces selected to complete formal looks.",
                ImageUrl = BeltProducts[0].ImageUrl,
                DisplayOrder = BeltsDisplayOrder,
                IsActive = true,
                IsDeleted = false,
                SeoTitle = "Men's Belts | Rich House",
                SeoDescription = "Premium men's belts from Rich House designed to complete tailoring and occasionwear."
            };

            dbContext.Categories.Add(category);
            await dbContext.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Created the Rich House Belts category for production rollout.");
        }
        else
        {
            var categoryUpdated = false;

            if (!category.IsActive)
            {
                category.IsActive = true;
                categoryUpdated = true;
            }

            if (category.IsDeleted)
            {
                category.IsDeleted = false;
                categoryUpdated = true;
            }

            if (string.IsNullOrWhiteSpace(category.Slug))
            {
                category.Slug = BeltsCategorySlug;
                categoryUpdated = true;
            }

            if (categoryUpdated)
            {
                await dbContext.SaveChangesAsync(cancellationToken);
                logger.LogInformation("Reactivated the existing Belts category for the Rich House production rollout.");
            }
        }

        var availableSizeIds = await dbContext.Sizes
            .Where(size => BeltSizeIds.Contains(size.Id))
            .Select(size => size.Id)
            .ToListAsync(cancellationToken);

        if (availableSizeIds.Count != BeltSizeIds.Length)
        {
            throw new InvalidOperationException("The Rich House belts seed requires sizes 40, 42, and 44 to exist.");
        }

        foreach (var seed in BeltProducts)
        {
            var exists = await dbContext.Products
                .IgnoreQueryFilters()
                .AnyAsync(
                    product => product.Slug == seed.Slug || product.ImportKey == seed.ImportKey,
                    cancellationToken);

            if (exists)
            {
                continue;
            }

            var product = new Product
            {
                Name = seed.Name,
                Slug = seed.Slug,
                Status = "Published",
                ShortDescription = seed.ShortDescription,
                Description = seed.Description,
                Price = seed.Price,
                CompareAtPrice = seed.CompareAtPrice,
                OfferPrice = null,
                OfferStart = null,
                OfferEnd = null,
                StockQuantity = seed.StockQuantity,
                CategoryId = category.Id,
                IsActive = true,
                IsFeatured = false,
                IsBestSeller = false,
                IsNewArrival = true,
                SeoTitle = $"{seed.Name} | Rich House",
                SeoDescription = seed.ShortDescription,
                ImportKey = seed.ImportKey,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };

            foreach (var sizeId in BeltSizeIds)
            {
                product.ProductSizes.Add(new ProductSize
                {
                    SizeId = sizeId
                });
            }

            var variantStocks = DistributeStock(seed.StockQuantity, BeltSizeIds.Length);
            for (var index = 0; index < BeltSizeIds.Length; index++)
            {
                var sizeId = BeltSizeIds[index];
                product.Variants.Add(new ProductVariant
                {
                    OptionKey = $"black:{sizeId}",
                    SizeId = sizeId,
                    ColorName = "Black",
                    ColorSlug = "black",
                    StockQuantity = variantStocks[index],
                    IsActive = variantStocks[index] > 0,
                    SortOrder = index
                });
            }

            product.Images.Add(new ProductImage
            {
                ImageUrl = seed.ImageUrl,
                IsMain = true,
                AltText = $"{seed.Name} by Rich House",
                SortOrder = 0,
                Width = seed.ImageWidth,
                Height = seed.ImageHeight,
                OriginalFileName = Path.GetFileName(seed.ImageUrl)
            });

            dbContext.Products.Add(product);
            logger.LogInformation("Queued seeded Rich House product {ProductSlug} for production rollout.", seed.Slug);
        }

        if (dbContext.ChangeTracker.HasChanges())
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private static int[] DistributeStock(int totalStock, int bucketCount)
    {
        var stockByBucket = new int[bucketCount];
        var baseQuantity = totalStock / bucketCount;
        var remainder = totalStock % bucketCount;

        for (var index = 0; index < bucketCount; index++)
        {
            stockByBucket[index] = baseQuantity + (index < remainder ? 1 : 0);
        }

        return stockByBucket;
    }

    private sealed record BeltProductSeed(
        string Name,
        string Slug,
        string ShortDescription,
        string Description,
        decimal Price,
        decimal? CompareAtPrice,
        int StockQuantity,
        string ImageUrl,
        int ImageWidth,
        int ImageHeight,
        string ImportKey);
}
