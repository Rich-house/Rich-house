using System.IO.Compression;
using System.Security.Cryptography;
using Marketify.Date;
using Marketify.Entites;
using ImageMagick;
using Microsoft.EntityFrameworkCore;

namespace Marketify.CatalogImport;

public sealed class RichHouseCatalogImporter(
    ApplicationDbContext dbContext,
    IWebHostEnvironment environment,
    ILogger<RichHouseCatalogImporter> logger) : IRichHouseCatalogImporter
{
    private readonly ApplicationDbContext _dbContext = dbContext;
    private readonly IWebHostEnvironment _environment = environment;
    private readonly ILogger<RichHouseCatalogImporter> _logger = logger;

    public async Task<RichHouseCatalogImportSummary> ImportAsync(
        RichHouseImportCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(command.ZipPath);

        var summary = new RichHouseCatalogImportSummary();
        var assetsRootPath = Path.GetFullPath(command.AssetsRootPath);
        var extractedSourcePath = Path.Combine(
            assetsRootPath,
            "source",
            SanitizeFileName(Path.GetFileNameWithoutExtension(command.ZipPath)));

        Directory.CreateDirectory(assetsRootPath);
        Directory.CreateDirectory(extractedSourcePath);

        var sourceAssets = await ExtractAndProfileSourceAssetsAsync(command.ZipPath, extractedSourcePath, summary, cancellationToken);

        var assignedSourceFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var sizesByName = await _dbContext.Sizes
            .AsNoTracking()
            .ToDictionaryAsync(size => size.Name, StringComparer.OrdinalIgnoreCase, cancellationToken);

        var manifestCategories = RichHouseCatalogManifest.Categories;
        var categoriesBySlug = await UpsertCategoriesAsync(manifestCategories, cancellationToken);
        summary.CategoriesActivated = categoriesBySlug.Count;

        var existingProducts = await _dbContext.Products
            .IgnoreQueryFilters()
            .Include(product => product.Images)
            .Include(product => product.ProductSizes)
            .Include(product => product.Variants)
            .AsSplitQuery()
            .ToListAsync(cancellationToken);
        var productsByImportKey = BuildLookup(
            existingProducts,
            product => product.ImportKey,
            "product import key");
        var productsBySlug = BuildLookup(
            existingProducts,
            product => product.Slug,
            "product slug");

        foreach (var productSeed in RichHouseCatalogManifest.Products)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!categoriesBySlug.TryGetValue(productSeed.CategorySlug, out var category))
            {
                throw new InvalidOperationException($"Category slug '{productSeed.CategorySlug}' is not defined in the Rich House manifest.");
            }

            if (!productsByImportKey.TryGetValue(productSeed.ImportKey, out var product)
                && !productsBySlug.TryGetValue(productSeed.Slug, out product))
            {
                product = new Product();
                _dbContext.Products.Add(product);
            }

            productsByImportKey[productSeed.ImportKey] = product;
            productsBySlug[productSeed.Slug] = product;
            MapProduct(product, productSeed, category.Id);
            SyncProductSizes(product, productSeed, sizesByName);
            SyncProductVariants(product, productSeed, sizesByName);

            var desiredHashes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var desiredImageOrder = new List<(SourceImageAsset Asset, string DisplayUrl, int SortOrder)>();
            var productDisplayDirectory = Path.Combine(_environment.WebRootPath, "catalog", "products", productSeed.Slug);
            var productThumbnailDirectory = Path.Combine(_environment.WebRootPath, "catalog", "thumbnails", productSeed.Slug);

            Directory.CreateDirectory(productDisplayDirectory);
            Directory.CreateDirectory(productThumbnailDirectory);

            for (var index = 0; index < productSeed.SourceFiles.Count; index++)
            {
                var sourceFileName = productSeed.SourceFiles[index];
                assignedSourceFiles.Add(sourceFileName);

                if (!sourceAssets.TryGetValue(sourceFileName, out var sourceAsset))
                {
                    throw new InvalidOperationException($"The source image '{sourceFileName}' was not found in the provided zip.");
                }

                if (sourceAsset.IsCorrupted)
                {
                    throw new InvalidOperationException($"The source image '{sourceFileName}' could not be decoded and cannot be imported.");
                }

                desiredHashes.Add(sourceAsset.Hash);

                var fileSlug = $"{productSeed.Slug}-{index + 1:D2}-{sourceAsset.Hash[..10]}";
                var displayFilePath = Path.Combine(productDisplayDirectory, $"{fileSlug}.webp");
                var thumbnailFilePath = Path.Combine(productThumbnailDirectory, $"{fileSlug}.webp");

                summary.OptimizedImagesGenerated += await GenerateWebpDerivativeAsync(
                    sourceAsset.OriginalPath,
                    displayFilePath,
                    maxDimension: 1600,
                    quality: 82,
                    cancellationToken);

                summary.ThumbnailsGenerated += await GenerateWebpDerivativeAsync(
                    sourceAsset.OriginalPath,
                    thumbnailFilePath,
                    maxDimension: 480,
                    quality: 72,
                    cancellationToken);

                desiredImageOrder.Add((
                    sourceAsset,
                    $"/catalog/products/{productSeed.Slug}/{Path.GetFileName(displayFilePath)}",
                    index));
            }

            SyncProductImages(product, productSeed.Name, desiredImageOrder);
            summary.AssignedImages += desiredImageOrder.Count;
            summary.ProductsImported++;
            summary.Products.Add(new ImportedProductReportItem(
                productSeed.Name,
                category.Name,
                productSeed.ColorName,
                desiredImageOrder.Count,
                productSeed.Price));
        }

        await DeactivateLegacyCategoriesAsync(categoriesBySlug.Keys, cancellationToken);
        await AssignCategoryImagesAsync(categoriesBySlug, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        foreach (var sourceFileName in sourceAssets.Keys.OrderBy(name => name, StringComparer.OrdinalIgnoreCase))
        {
            if (!assignedSourceFiles.Contains(sourceFileName))
            {
                summary.UnassignedFiles.Add(sourceFileName);
            }
        }

        if (!string.IsNullOrWhiteSpace(command.ReportPath))
        {
            await WriteMarkdownReportAsync(command.ReportPath, summary, cancellationToken);
        }

        return summary;
    }

    private async Task<Dictionary<string, SourceImageAsset>> ExtractAndProfileSourceAssetsAsync(
        string zipPath,
        string extractedSourcePath,
        RichHouseCatalogImportSummary summary,
        CancellationToken cancellationToken)
    {
        var assets = new Dictionary<string, SourceImageAsset>(StringComparer.OrdinalIgnoreCase);
        var hashes = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

        using var archive = ZipFile.OpenRead(zipPath);
        foreach (var entry in archive.Entries.Where(entry => IsImageFile(entry.Name)))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var fileName = entry.Name.Trim();
            var destinationPath = Path.Combine(extractedSourcePath, fileName);
            Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);

            await using (var entryStream = entry.Open())
            await using (var destinationStream = File.Create(destinationPath))
            {
                await entryStream.CopyToAsync(destinationStream, cancellationToken);
            }

            var hash = await ComputeSha256Async(destinationPath, cancellationToken);
            var imageInfo = TryIdentifyImage(destinationPath);
            var isCorrupted = imageInfo is null;

            summary.SourceFilesFound++;
            if (isCorrupted)
            {
                summary.CorruptedFilesDetected++;
                summary.CorruptedFiles.Add(fileName);
            }

            if (!hashes.TryGetValue(hash, out var files))
            {
                files = [];
                hashes[hash] = files;
            }

            files.Add(fileName);

            assets[fileName] = new SourceImageAsset(
                fileName,
                destinationPath,
                hash,
                imageInfo is null ? null : (int?)imageInfo.Width,
                imageInfo is null ? null : (int?)imageInfo.Height,
                isCorrupted);
        }

        foreach (var duplicateGroup in hashes.Where(group => group.Value.Count > 1))
        {
            summary.DuplicateGroups.Add(new DuplicateAssetGroup(
                duplicateGroup.Key,
                duplicateGroup.Value
                    .OrderBy(fileName => fileName, StringComparer.OrdinalIgnoreCase)
                    .ToList()));
        }

        return assets;
    }

    private async Task<Dictionary<string, Category>> UpsertCategoriesAsync(
        IReadOnlyList<RichHouseCategorySeedDefinition> categorySeeds,
        CancellationToken cancellationToken)
    {
        var existingCategories = await _dbContext.Categories
            .IgnoreQueryFilters()
            .ToListAsync(cancellationToken);
        var existingCategoriesBySlug = BuildLookup(
            existingCategories,
            category => category.Slug,
            "category slug");
        var existingCategoriesByName = BuildLookup(
            existingCategories,
            category => category.Name,
            "category name");

        var categoriesBySlug = new Dictionary<string, Category>(StringComparer.OrdinalIgnoreCase);
        foreach (var seed in categorySeeds)
        {
            if (!existingCategoriesBySlug.TryGetValue(seed.Slug, out var category)
                && !existingCategoriesByName.TryGetValue(seed.Name, out category))
            {
                category = new Category();
                _dbContext.Categories.Add(category);
            }

            category.Name = seed.Name;
            category.Slug = seed.Slug;
            category.Description = seed.Description;
            category.DisplayOrder = seed.DisplayOrder;
            category.IsActive = true;
            category.IsDeleted = false;
            category.SeoTitle = seed.SeoTitle ?? $"{seed.Name} | Rich House";
            category.SeoDescription = seed.SeoDescription ?? seed.Description;

            existingCategoriesBySlug[seed.Slug] = category;
            existingCategoriesByName[seed.Name] = category;
            categoriesBySlug[seed.Slug] = category;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return categoriesBySlug;
    }

    private static void MapProduct(Product product, RichHouseProductSeedDefinition seed, int categoryId)
    {
        product.Name = seed.Name;
        product.Slug = seed.Slug;
        product.Sku = seed.Sku;
        product.Barcode = null;
        product.Status = "Published";
        product.ShortDescription = seed.ShortDescription;
        product.Description = seed.Description;
        product.Price = seed.Price;
        product.CompareAtPrice = seed.CompareAtPrice;
        product.OfferPrice = seed.OfferPrice;
        product.OfferStart = seed.OfferStart;
        product.OfferEnd = seed.OfferEnd;
        product.CategoryId = categoryId;
        product.IsActive = true;
        product.IsFeatured = seed.IsFeatured;
        product.IsBestSeller = seed.IsBestSeller;
        product.IsNewArrival = seed.IsNewArrival;
        product.SeoTitle = $"{seed.Name} | Rich House";
        product.SeoDescription = seed.ShortDescription;
        product.ImportKey = seed.ImportKey;
        product.UpdatedAt = DateTimeOffset.UtcNow;
    }

    private static void SyncProductSizes(
        Product product,
        RichHouseProductSeedDefinition seed,
        IReadOnlyDictionary<string, Size> sizesByName)
    {
        var desiredSizeIds = seed.Variants
            .Select(variant =>
            {
                if (!sizesByName.TryGetValue(variant.SizeName, out var size))
                {
                    throw new InvalidOperationException($"Size '{variant.SizeName}' is not seeded in the database.");
                }

                return size.Id;
            })
            .Distinct()
            .ToHashSet();

        var existingSizeIds = product.ProductSizes.Select(productSize => productSize.SizeId).ToHashSet();

        foreach (var removedSize in product.ProductSizes.Where(productSize => !desiredSizeIds.Contains(productSize.SizeId)).ToList())
        {
            product.ProductSizes.Remove(removedSize);
        }

        foreach (var desiredSizeId in desiredSizeIds.Where(sizeId => !existingSizeIds.Contains(sizeId)))
        {
            product.ProductSizes.Add(new ProductSize
            {
                SizeId = desiredSizeId
            });
        }
    }

    private static void SyncProductVariants(
        Product product,
        RichHouseProductSeedDefinition seed,
        IReadOnlyDictionary<string, Size> sizesByName)
    {
        var variantsByOptionKey = product.Variants
            .Where(variant => !string.IsNullOrWhiteSpace(variant.OptionKey))
            .GroupBy(variant => variant.OptionKey!, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);

        var desiredOptionKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var sortOrder = 0;
        var totalStock = 0;

        foreach (var variantSeed in seed.Variants)
        {
            if (!sizesByName.TryGetValue(variantSeed.SizeName, out var size))
            {
                throw new InvalidOperationException($"Size '{variantSeed.SizeName}' is not seeded in the database.");
            }

            var optionKey = $"{seed.ColorSlug}:{size.Name}".ToLowerInvariant();
            desiredOptionKeys.Add(optionKey);

            if (!variantsByOptionKey.TryGetValue(optionKey, out var variant))
            {
                variant = new ProductVariant();
                product.Variants.Add(variant);
            }

            variant.OptionKey = optionKey;
            variant.SizeId = size.Id;
            variant.ColorName = seed.ColorName;
            variant.ColorSlug = seed.ColorSlug;
            variant.Sku = $"{seed.Sku}-{size.Name}";
            variant.Barcode = null;
            variant.PriceOverride = variantSeed.PriceOverride;
            variant.StockQuantity = variantSeed.StockQuantity;
            variant.IsActive = variantSeed.StockQuantity > 0;
            variant.SortOrder = sortOrder++;
            totalStock += variantSeed.StockQuantity;
        }

        foreach (var removedVariant in product.Variants.Where(variant => !desiredOptionKeys.Contains(variant.OptionKey)).ToList())
        {
            product.Variants.Remove(removedVariant);
        }

        product.StockQuantity = totalStock;
    }

    private static void SyncProductImages(
        Product product,
        string productName,
        IReadOnlyList<(SourceImageAsset Asset, string DisplayUrl, int SortOrder)> desiredImages)
    {
        var desiredHashes = desiredImages
            .Select(item => item.Asset.Hash)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var removedImage in product.Images.Where(image => image.SourceHash is null || !desiredHashes.Contains(image.SourceHash)).ToList())
        {
            product.Images.Remove(removedImage);
        }

        foreach (var desiredImage in desiredImages)
        {
            var existingImage = product.Images.SingleOrDefault(image =>
                string.Equals(image.SourceHash, desiredImage.Asset.Hash, StringComparison.OrdinalIgnoreCase));

            if (existingImage is null)
            {
                existingImage = new ProductImage();
                product.Images.Add(existingImage);
            }

            existingImage.ImageUrl = desiredImage.DisplayUrl;
            existingImage.IsMain = desiredImage.SortOrder == 0;
            existingImage.SortOrder = desiredImage.SortOrder;
            existingImage.AltText = BuildAltText(productName, desiredImage.SortOrder);
            existingImage.Width = desiredImage.Asset.Width;
            existingImage.Height = desiredImage.Asset.Height;
            existingImage.OriginalFileName = desiredImage.Asset.FileName;
            existingImage.SourceHash = desiredImage.Asset.Hash;
        }
    }

    private async Task DeactivateLegacyCategoriesAsync(
        IEnumerable<string> activeSlugs,
        CancellationToken cancellationToken)
    {
        var activeSlugSet = activeSlugs.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var legacyCategories = await _dbContext.Categories
            .IgnoreQueryFilters()
            .Include(category => category.Products)
            .Where(category => category.Slug == null || !activeSlugSet.Contains(category.Slug))
            .ToListAsync(cancellationToken);

        foreach (var category in legacyCategories)
        {
            if (category.Products.Count == 0)
            {
                category.IsActive = false;
            }
        }
    }

    private async Task AssignCategoryImagesAsync(
        IReadOnlyDictionary<string, Category> categoriesBySlug,
        CancellationToken cancellationToken)
    {
        foreach (var (categorySlug, productImportKey) in RichHouseCatalogManifest.CategoryCoverProductKeys)
        {
            if (!categoriesBySlug.TryGetValue(categorySlug, out var category))
            {
                continue;
            }

            var product = await _dbContext.Products
                .AsNoTracking()
                .Include(item => item.Images)
                .SingleOrDefaultAsync(item => item.ImportKey == productImportKey, cancellationToken);

            var coverImage = product?.Images
                .OrderBy(image => image.SortOrder)
                .FirstOrDefault(image => image.IsMain || image.SortOrder == 0);

            if (coverImage is not null)
            {
                category.ImageUrl = coverImage.ImageUrl;
            }
        }
    }

    private async Task<int> GenerateWebpDerivativeAsync(
        string sourceFilePath,
        string outputFilePath,
        int maxDimension,
        int quality,
        CancellationToken cancellationToken)
    {
        if (File.Exists(outputFilePath))
        {
            return 0;
        }

        await Task.Run(() =>
        {
            using var image = new MagickImage(sourceFilePath);
            image.AutoOrient();

            if (image.Width > maxDimension || image.Height > maxDimension)
            {
                image.Resize(new MagickGeometry((uint)maxDimension, (uint)maxDimension)
                {
                    IgnoreAspectRatio = false
                });
            }

            image.Format = MagickFormat.WebP;
            image.Quality = (uint)quality;
            image.Write(outputFilePath);
        }, cancellationToken);

        return 1;
    }

    private async Task WriteMarkdownReportAsync(
        string reportPath,
        RichHouseCatalogImportSummary summary,
        CancellationToken cancellationToken)
    {
        var directory = Path.GetDirectoryName(Path.GetFullPath(reportPath));
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await using var writer = new StreamWriter(reportPath, false);
        await writer.WriteLineAsync("# IMAGE_IMPORT_REPORT");
        await writer.WriteLineAsync();
        await writer.WriteLineAsync($"- Files found: {summary.SourceFilesFound}");
        await writer.WriteLineAsync($"- Products inferred: {summary.ProductsImported}");
        await writer.WriteLineAsync($"- Categories inferred: {summary.CategoriesActivated}");
        await writer.WriteLineAsync($"- Assigned product images: {summary.AssignedImages}");
        await writer.WriteLineAsync($"- Optimized WebP images generated: {summary.OptimizedImagesGenerated}");
        await writer.WriteLineAsync($"- Thumbnail images generated: {summary.ThumbnailsGenerated}");
        await writer.WriteLineAsync($"- Corrupted files: {summary.CorruptedFilesDetected}");
        await writer.WriteLineAsync();
        await writer.WriteLineAsync("## Generated Sizes");
        await writer.WriteLineAsync();
        await writer.WriteLineAsync("- Display derivatives: WebP, max 1600px on the longest edge");
        await writer.WriteLineAsync("- Thumbnails: WebP, max 480px on the longest edge");
        await writer.WriteLineAsync("- Originals preserved: extracted to the configured `assets-root/source/...` folder without editing the supplied archive");
        await writer.WriteLineAsync();
        await writer.WriteLineAsync("## Products Inferred");
        await writer.WriteLineAsync();

        foreach (var product in summary.Products.OrderBy(item => item.Category).ThenBy(item => item.Name, StringComparer.OrdinalIgnoreCase))
        {
            await writer.WriteLineAsync($"- {product.Name} | Category: {product.Category} | Color guess: {product.Color} | Images: {product.ImageCount} | Provisional price: EGP {product.Price:N0}");
        }

        await writer.WriteLineAsync();
        await writer.WriteLineAsync("## Duplicate Files");
        await writer.WriteLineAsync();
        if (summary.DuplicateGroups.Count == 0)
        {
            await writer.WriteLineAsync("- No duplicate file hashes were detected.");
        }
        else
        {
            foreach (var duplicateGroup in summary.DuplicateGroups.OrderBy(group => group.Files[0], StringComparer.OrdinalIgnoreCase))
            {
                await writer.WriteLineAsync($"- Hash `{duplicateGroup.Hash}`");
                foreach (var file in duplicateGroup.Files)
                {
                    await writer.WriteLineAsync($"  {file}");
                }
            }
        }

        await writer.WriteLineAsync();
        await writer.WriteLineAsync("## Corrupted Files");
        await writer.WriteLineAsync();
        if (summary.CorruptedFiles.Count == 0)
        {
            await writer.WriteLineAsync("- No corrupted image files were detected during decode.");
        }
        else
        {
            foreach (var file in summary.CorruptedFiles.OrderBy(file => file, StringComparer.OrdinalIgnoreCase))
            {
                await writer.WriteLineAsync($"- {file}");
            }
        }

        await writer.WriteLineAsync();
        await writer.WriteLineAsync("## Unassigned Images");
        await writer.WriteLineAsync();
        if (summary.UnassignedFiles.Count == 0)
        {
            await writer.WriteLineAsync("- All source files were assigned to catalog products.");
        }
        else
        {
            foreach (var file in summary.UnassignedFiles.OrderBy(file => file, StringComparer.OrdinalIgnoreCase))
            {
                await writer.WriteLineAsync($"- {file}");
            }
        }

        await writer.WriteLineAsync();
        await writer.WriteLineAsync("## Assumptions Requiring Review");
        await writer.WriteLineAsync();
        await writer.WriteLineAsync("- Several tailored looks were grouped by visual similarity and shooting sequence; final merchandising should confirm whether some light-grey and beige looks are separate SKUs or alternate styling of the same suit.");
        await writer.WriteLineAsync("- The purple graphic T-shirt was imported as a casual placeholder because it appears in the asset set, but it may not match the final premium menswear assortment and can be disabled or removed from the admin panel later.");
        await writer.WriteLineAsync("- Category cover images were inferred from representative product images because no dedicated category banners were supplied in the archive.");
        await writer.WriteLineAsync("- A subset of phone images appears to be signage, interior, or detail photography and was intentionally left unassigned rather than forced into product records.");
        await writer.FlushAsync(cancellationToken);
    }

    private static string BuildAltText(string productName, int sortOrder)
    {
        return sortOrder switch
        {
            0 => $"{productName} front view",
            1 => $"{productName} detail view",
            _ => $"{productName} image {sortOrder + 1}"
        };
    }

    private static async Task<string> ComputeSha256Async(string filePath, CancellationToken cancellationToken)
    {
        await using var stream = File.OpenRead(filePath);
        var hashBytes = await SHA256.HashDataAsync(stream, cancellationToken);
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    private static MagickImageInfo? TryIdentifyImage(string filePath)
    {
        try
        {
            return new MagickImageInfo(filePath);
        }
        catch
        {
            return null;
        }
    }

    private static bool IsImageFile(string fileName)
    {
        var extension = Path.GetExtension(fileName);
        return extension.Equals(".jpg", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".jpeg", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".png", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".webp", StringComparison.OrdinalIgnoreCase);
    }

    private static string SanitizeFileName(string value)
    {
        var invalidCharacters = Path.GetInvalidFileNameChars();
        var sanitized = new string(value
            .Select(character => invalidCharacters.Contains(character) ? '-' : character)
            .ToArray());

        return sanitized
            .Replace(' ', '-')
            .Trim('-')
            .ToLowerInvariant();
    }

    private Dictionary<string, TEntity> BuildLookup<TEntity>(
        IEnumerable<TEntity> items,
        Func<TEntity, string?> keySelector,
        string keyName)
        where TEntity : class
    {
        var lookup = new Dictionary<string, TEntity>(StringComparer.OrdinalIgnoreCase);

        foreach (var item in items)
        {
            var key = keySelector(item)?.Trim();
            if (string.IsNullOrWhiteSpace(key))
            {
                continue;
            }

            if (!lookup.TryAdd(key, item))
            {
                _logger.LogWarning(
                    "Duplicate existing {KeyName} '{KeyValue}' was found during Rich House import. The first record will be reused and the duplicate ignored.",
                    keyName,
                    key);
            }
        }

        return lookup;
    }

    private sealed record SourceImageAsset(
        string FileName,
        string OriginalPath,
        string Hash,
        int? Width,
        int? Height,
        bool IsCorrupted);
}
