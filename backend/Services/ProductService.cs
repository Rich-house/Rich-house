using Marketify.Contracts.Common;
using Marketify.Contracts.Product;
using Marketify.Date;
using Marketify.Entites;
using Marketify.Helper;
using Microsoft.EntityFrameworkCore;

namespace Marketify.Services;

public class ProductService(
    ApplicationDbContext context,
    CatalogImageStorageService imageStorage) : IProductService
{
    private readonly ApplicationDbContext _context = context;
    private readonly CatalogImageStorageService _imageStorage = imageStorage;

    public async Task<PagedResult<AdminProductListItemDto>> GetAdminProductsAsync(AdminProductQuery query, CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 50);
        var activeOffers = await GetActiveOfferSnapshotsAsync(now, cancellationToken);

        var productsQuery = _context.Products
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Include(product => product.Category)
            .Include(product => product.Images)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            productsQuery = productsQuery.Where(product =>
                product.Name.Contains(search)
                || (product.Slug != null && product.Slug.Contains(search))
                || (product.Sku != null && product.Sku.Contains(search)));
        }

        if (query.CategoryId is not null)
        {
            productsQuery = productsQuery.Where(product => product.CategoryId == query.CategoryId.Value);
        }

        if (query.IsActive is not null)
        {
            productsQuery = productsQuery.Where(product => product.IsActive == query.IsActive.Value);
        }

        if (query.LowStockOnly == true)
        {
            productsQuery = productsQuery.Where(product => product.StockQuantity > 0 && product.StockQuantity <= 5);
        }

        var totalItems = await productsQuery.CountAsync(cancellationToken);
        var products = await productsQuery
            .OrderByDescending(product => product.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var items = products
            .Select(product =>
            {
                var linkedOfferPrice = ComputeLinkedOfferPrice(product.Price, product.Id, product.CategoryId, activeOffers);
                var directOfferIsActive = IsDirectOfferActive(product.OfferPrice, product.OfferStart, product.OfferEnd, now);
                var salePrice = directOfferIsActive
                    ? product.OfferPrice
                    : linkedOfferPrice;

                return new AdminProductListItemDto(
                    product.Id,
                    product.Name,
                    product.Slug,
                    product.CategoryId,
                    product.Category?.Name ?? "Uncategorized",
                    NormalizeImagePath(product.Images
                        .OrderBy(image => image.SortOrder)
                        .ThenByDescending(image => image.IsMain)
                        .Select(image => image.ImageUrl)
                        .FirstOrDefault() ?? string.Empty),
                    product.Price,
                    product.CompareAtPrice,
                    salePrice,
                    product.StockQuantity,
                    product.Status,
                    product.IsActive,
                    product.IsFeatured,
                    product.IsBestSeller,
                    product.IsNewArrival);
            })
            .ToList();

        var totalPages = totalItems == 0 ? 0 : (int)Math.Ceiling(totalItems / (double)pageSize);
        return new PagedResult<AdminProductListItemDto>(
            items,
            new PaginationMeta(page, pageSize, totalItems, totalPages, page > 1, totalPages > 0 && page < totalPages));
    }

    public async Task<AdminProductDetailsDto?> GetAdminProductByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var product = await _context.Products
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Include(item => item.Images)
            .Include(item => item.ProductSizes)
                .ThenInclude(productSize => productSize.Size)
            .Include(item => item.Variants)
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (product is null)
        {
            return null;
        }

        return new AdminProductDetailsDto(
            product.Id,
            product.Name,
            product.Slug ?? string.Empty,
            product.ShortDescription,
            product.Description,
            product.CategoryId,
            product.Price,
            product.CompareAtPrice,
            product.OfferPrice,
            product.OfferStart,
            product.OfferEnd,
            product.StockQuantity,
            product.Status,
            product.IsActive,
            product.IsFeatured,
            product.IsBestSeller,
            product.IsNewArrival,
            product.ProductSizes
                .OrderBy(productSize => productSize.Size!.SortOrder)
                .Select(productSize => productSize.SizeId)
                .ToList(),
            product.Variants
                .Select(variant => variant.ColorName?.Trim())
                .Where(color => !string.IsNullOrWhiteSpace(color))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Cast<string>()
                .ToList(),
            product.Images
                .OrderBy(image => image.SortOrder)
                .ThenByDescending(image => image.IsMain)
                .Select(image => new AdminProductImageDto(
                    image.Id,
                    NormalizeImagePath(image.ImageUrl),
                    image.IsMain,
                    image.SortOrder))
                .ToList());
    }

    public async Task<AdminProductDetailsDto> CreateAdminProductAsync(UpsertProductDto dto, CancellationToken cancellationToken = default)
    {
        ValidateAdminProduct(dto, true);
        await EnsureCategoryExistsAsync(dto.CategoryId, cancellationToken);
        await EnsureSizesExistAsync(dto.SelectedSizeIds, cancellationToken);

        var product = new Product();
        MapProductFields(product, dto);
        product.Slug = await BuildUniqueSlugAsync(dto.Slug, dto.Name, null, cancellationToken);
        product.CreatedAt = DateTimeOffset.UtcNow;
        product.UpdatedAt = product.CreatedAt;

        var savedImages = await SaveNewImagesAsync(dto, product.Slug, cancellationToken);

        try
        {
            ApplySizesAndVariants(product, dto);
            ApplyImages(product, dto, [], savedImages);

            _context.Products.Add(product);
            await _context.SaveChangesAsync(cancellationToken);

            return await GetAdminProductByIdAsync(product.Id, cancellationToken)
                ?? throw new InvalidOperationException("Product could not be loaded after saving.");
        }
        catch
        {
            CleanupSavedImages(savedImages);
            throw;
        }
    }

    public async Task<AdminProductDetailsDto> UpdateAdminProductAsync(int id, UpsertProductDto dto, CancellationToken cancellationToken = default)
    {
        ValidateAdminProduct(dto, false);
        await EnsureCategoryExistsAsync(dto.CategoryId, cancellationToken);
        await EnsureSizesExistAsync(dto.SelectedSizeIds, cancellationToken);

        var product = await _context.Products
            .IgnoreQueryFilters()
            .Include(item => item.Images)
            .Include(item => item.ProductSizes)
            .Include(item => item.Variants)
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (product is null)
        {
            throw new InvalidOperationException("Product not found.");
        }

        MapProductFields(product, dto);
        product.Slug = await BuildUniqueSlugAsync(dto.Slug, dto.Name, id, cancellationToken);
        product.UpdatedAt = DateTimeOffset.UtcNow;

        var savedImages = await SaveNewImagesAsync(dto, product.Slug, cancellationToken);
        var previousImageUrls = product.Images
            .Select(image => NormalizeImagePath(image.ImageUrl))
            .ToList();

        try
        {
            ApplySizesAndVariants(product, dto);
            var keptExistingUrls = ApplyImages(product, dto, previousImageUrls, savedImages);

            await _context.SaveChangesAsync(cancellationToken);

            foreach (var imageUrl in previousImageUrls.Except(keptExistingUrls, StringComparer.OrdinalIgnoreCase))
            {
                _imageStorage.DeleteIfExists(imageUrl);
            }

            return await GetAdminProductByIdAsync(product.Id, cancellationToken)
                ?? throw new InvalidOperationException("Product could not be loaded after saving.");
        }
        catch
        {
            CleanupSavedImages(savedImages);
            throw;
        }
    }

    public async Task DeleteProductAsync(int id, CancellationToken cancellationToken = default)
    {
        var product = await _context.Products
            .IgnoreQueryFilters()
            .Include(item => item.Images)
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (product is null)
        {
            throw new InvalidOperationException("Product not found.");
        }

        var imageUrls = product.Images.Select(image => image.ImageUrl).ToList();
        _context.Products.Remove(product);
        await _context.SaveChangesAsync(cancellationToken);

        foreach (var imageUrl in imageUrls)
        {
            _imageStorage.DeleteIfExists(imageUrl);
        }
    }

    public async Task<ProductResponseDto?> GetByID(int id)
    {
        var currentTime = DateTimeOffset.UtcNow;
        var activeOffers = await GetActiveOfferSnapshotsAsync(currentTime, CancellationToken.None);

        var product = await _context.Products
            .AsNoTracking()
            .Where(item => item.IsActive && item.Category.IsActive)
            .Include(item => item.Category)
            .Include(item => item.Images)
            .Include(item => item.ProductSizes)
                .ThenInclude(productSize => productSize.Size)
            .Include(item => item.Variants)
                .ThenInclude(variant => variant.Size)
            .AsSplitQuery()
            .SingleOrDefaultAsync(item => item.Id == id);

        if (product is null)
        {
            return null;
        }

        return MapPublicProductDetails(product, currentTime, activeOffers);
    }

    public async Task<ProductResponseDto?> GetBySlugAsync(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug))
        {
            return null;
        }

        var currentTime = DateTimeOffset.UtcNow;
        var activeOffers = await GetActiveOfferSnapshotsAsync(currentTime, CancellationToken.None);
        var normalizedSlug = slug.Trim();

        var product = await _context.Products
            .AsNoTracking()
            .Where(item => item.IsActive && item.Category.IsActive)
            .Include(item => item.Category)
            .Include(item => item.Images)
            .Include(item => item.ProductSizes)
                .ThenInclude(productSize => productSize.Size)
            .Include(item => item.Variants)
                .ThenInclude(variant => variant.Size)
            .AsSplitQuery()
            .SingleOrDefaultAsync(item => item.Slug == normalizedSlug);

        return product is null ? null : MapPublicProductDetails(product, currentTime, activeOffers);
    }

    public async Task<PagedResult<ProductReadDto>> GetCatalogAsync(ProductCatalogQuery query)
    {
        var currentTime = DateTimeOffset.UtcNow;
        var activeOffers = await GetActiveOfferSnapshotsAsync(currentTime, CancellationToken.None);
        var page = Math.Max(query.Page, 1);
        var pageSize = Math.Clamp(query.PageSize, 1, 48);

        var productsQuery = BuildCatalogQuery(query, currentTime, activeOffers);
        var totalItems = await productsQuery.CountAsync();
        var sortedQuery = ApplyCatalogSort(productsQuery, query.Sort, currentTime);
        var pagedQuery = sortedQuery
            .Skip((page - 1) * pageSize)
            .Take(pageSize);
        var items = await ProjectCatalogItemsAsync(pagedQuery, currentTime, activeOffers);

        var totalPages = totalItems == 0 ? 0 : (int)Math.Ceiling(totalItems / (double)pageSize);
        return new PagedResult<ProductReadDto>(
            items,
            new PaginationMeta(page, pageSize, totalItems, totalPages, page > 1, totalPages > 0 && page < totalPages));
    }

    public async Task<IEnumerable<ProductReadDto>> GetAllProductsAsync()
    {
        var currentTime = DateTimeOffset.UtcNow;
        var activeOffers = await GetActiveOfferSnapshotsAsync(currentTime, CancellationToken.None);
        var productsQuery = ApplyCatalogSort(
            BuildCatalogQuery(new ProductCatalogQuery(), currentTime, activeOffers),
            "newest",
            currentTime);

        return await ProjectCatalogItemsAsync(productsQuery, currentTime, activeOffers);
    }

    public async Task<IEnumerable<ProductReadDto>> GetProductByCategory(int? categoryId)
    {
        if (categoryId is null)
        {
            return [];
        }

        var currentTime = DateTimeOffset.UtcNow;
        var activeOffers = await GetActiveOfferSnapshotsAsync(currentTime, CancellationToken.None);
        var productsQuery = ApplyCatalogSort(
            BuildCatalogQuery(new ProductCatalogQuery { CategoryId = categoryId }, currentTime, activeOffers),
            "newest",
            currentTime);

        return await ProjectCatalogItemsAsync(productsQuery, currentTime, activeOffers);
    }

    public async Task<IEnumerable<ProductReadDto>> SearchProductsAsync(string searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            return [];
        }

        var currentTime = DateTimeOffset.UtcNow;
        var activeOffers = await GetActiveOfferSnapshotsAsync(currentTime, CancellationToken.None);
        var productsQuery = ApplyCatalogSort(
            BuildCatalogQuery(new ProductCatalogQuery { Search = searchTerm }, currentTime, activeOffers),
            "newest",
            currentTime);

        return await ProjectCatalogItemsAsync(productsQuery, currentTime, activeOffers);
    }

    private IQueryable<Product> BuildCatalogQuery(
        ProductCatalogQuery query,
        DateTimeOffset currentTime,
        IReadOnlyList<OfferSnapshot> activeOffers)
    {
        var offerProductIds = activeOffers.SelectMany(offer => offer.ProductIds).Distinct().ToArray();
        var offerCategoryIds = activeOffers.SelectMany(offer => offer.CategoryIds).Distinct().ToArray();

        var productsQuery = _context.Products
            .AsNoTracking()
            .Where(product => product.IsActive && product.Category.IsActive);

        if (query.CategoryId is not null)
        {
            productsQuery = productsQuery.Where(product => product.CategoryId == query.CategoryId.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.CategorySlug))
        {
            var categorySlug = query.CategorySlug.Trim();
            productsQuery = productsQuery.Where(product => product.Category.Slug == categorySlug);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var searchTerm = query.Search.Trim();
            productsQuery = productsQuery.Where(product =>
                product.Name.Contains(searchTerm)
                || (product.ShortDescription != null && product.ShortDescription.Contains(searchTerm))
                || product.Description.Contains(searchTerm));
        }

        if (!string.IsNullOrWhiteSpace(query.Size))
        {
            var sizeName = query.Size.Trim();
            productsQuery = productsQuery.Where(product =>
                product.ProductSizes.Any(productSize =>
                    productSize.Size != null && productSize.Size.Name == sizeName));
        }

        if (!string.IsNullOrWhiteSpace(query.Color))
        {
            var color = query.Color.Trim();
            productsQuery = productsQuery.Where(product =>
                product.Variants.Any(variant =>
                    variant.IsActive &&
                    (variant.ColorSlug == color || variant.ColorName == color)));
        }

        if (query.Featured is not null)
        {
            productsQuery = productsQuery.Where(product => product.IsFeatured == query.Featured.Value);
        }

        if (query.BestSeller is not null)
        {
            productsQuery = productsQuery.Where(product => product.IsBestSeller == query.BestSeller.Value);
        }

        if (query.NewArrival is not null)
        {
            productsQuery = productsQuery.Where(product => product.IsNewArrival == query.NewArrival.Value);
        }

        if (query.InStock is not null)
        {
            productsQuery = query.InStock.Value
                ? productsQuery.Where(product => product.StockQuantity > 0)
                : productsQuery.Where(product => product.StockQuantity <= 0);
        }

        if (query.OnOffer is not null)
        {
            productsQuery = query.OnOffer.Value
                ? productsQuery.Where(product =>
                    (product.OfferPrice.HasValue
                     && (!product.OfferStart.HasValue || product.OfferStart <= currentTime)
                     && (!product.OfferEnd.HasValue || product.OfferEnd >= currentTime))
                    || offerProductIds.Contains(product.Id)
                    || offerCategoryIds.Contains(product.CategoryId))
                : productsQuery.Where(product =>
                    (!product.OfferPrice.HasValue
                     || (product.OfferStart.HasValue && product.OfferStart > currentTime)
                     || (product.OfferEnd.HasValue && product.OfferEnd < currentTime))
                    && !offerProductIds.Contains(product.Id)
                    && !offerCategoryIds.Contains(product.CategoryId));
        }

        if (query.MinPrice is not null)
        {
            var minPrice = query.MinPrice.Value;
            productsQuery = productsQuery.Where(product =>
                (product.OfferPrice.HasValue
                 && (!product.OfferStart.HasValue || product.OfferStart <= currentTime)
                 && (!product.OfferEnd.HasValue || product.OfferEnd >= currentTime)
                    ? product.OfferPrice.Value
                    : product.Price) >= minPrice);
        }

        if (query.MaxPrice is not null)
        {
            var maxPrice = query.MaxPrice.Value;
            productsQuery = productsQuery.Where(product =>
                (product.OfferPrice.HasValue
                 && (!product.OfferStart.HasValue || product.OfferStart <= currentTime)
                 && (!product.OfferEnd.HasValue || product.OfferEnd >= currentTime)
                    ? product.OfferPrice.Value
                    : product.Price) <= maxPrice);
        }

        return productsQuery;
    }

    private async Task<List<ProductReadDto>> ProjectCatalogItemsAsync(
        IQueryable<Product> productsQuery,
        DateTimeOffset currentTime,
        IReadOnlyList<OfferSnapshot> activeOffers)
    {
        var items = await productsQuery
            .Include(product => product.Images)
            .Include(product => product.ProductSizes)
                .ThenInclude(productSize => productSize.Size)
            .AsSplitQuery()
            .Select(product => new ProductReadDto
            {
                Id = product.Id,
                CategoryId = product.CategoryId,
                Name = product.Name,
                Slug = product.Slug,
                ShortDescription = product.ShortDescription,
                Description = product.Description,
                StockQuantity = product.StockQuantity,
                Price = product.Price,
                CompareAtPrice = product.CompareAtPrice,
                OfferPrice = product.OfferPrice,
                OfferStart = product.OfferStart,
                OfferEnd = product.OfferEnd,
                Status = product.Status,
                IsFeatured = product.IsFeatured,
                IsBestSeller = product.IsBestSeller,
                IsNewArrival = product.IsNewArrival,
                IsOnOffer = false,
                CategoryName = product.Category != null ? product.Category.Name ?? "General" : "General",
                CategorySlug = product.Category != null ? product.Category.Slug : null,
                ImageUrls = product.Images
                    .OrderBy(image => image.SortOrder)
                    .ThenByDescending(image => image.IsMain)
                    .Select(image => NormalizeImagePath(image.ImageUrl))
                    .ToList(),
                Sizes = product.ProductSizes
                    .OrderBy(productSize => productSize.Size!.SortOrder)
                    .Select(productSize => productSize.Size!.Name)
                    .ToList()
            })
            .ToListAsync();

        foreach (var item in items)
        {
            ApplyDisplayPricing(item, currentTime, activeOffers);
        }

        return items;
    }

    private ProductResponseDto MapPublicProductDetails(
        Product product,
        DateTimeOffset currentTime,
        IReadOnlyList<OfferSnapshot> activeOffers)
    {
        var orderedImages = product.Images
            .OrderBy(image => image.SortOrder)
            .ThenByDescending(image => image.IsMain)
            .Select(image => NormalizeImagePath(image.ImageUrl))
            .ToList();

        var sizesList = product.ProductSizes
            .Select(productSize => productSize.Size?.Name)
            .Where(sizeName => !string.IsNullOrWhiteSpace(sizeName))
            .Select(sizeName => sizeName!)
            .ToList();

        var displayPrice = product.Price;
        var compareAtPrice = product.CompareAtPrice;
        var offerPrice = product.OfferPrice;
        var offerStart = product.OfferStart;
        var offerEnd = product.OfferEnd;

        var directOfferIsActive = IsDirectOfferActive(offerPrice, offerStart, offerEnd, currentTime);
        if (directOfferIsActive && offerPrice.HasValue)
        {
            displayPrice = offerPrice.Value;
            compareAtPrice = compareAtPrice is > 0m && compareAtPrice > displayPrice ? compareAtPrice : product.Price;
        }
        else
        {
            var linkedOfferPrice = ComputeLinkedOfferPrice(product.Price, product.Id, product.CategoryId, activeOffers);
            if (linkedOfferPrice is not null && linkedOfferPrice.Value < product.Price)
            {
                displayPrice = linkedOfferPrice.Value;
                compareAtPrice = compareAtPrice is > 0m && compareAtPrice > displayPrice ? compareAtPrice : product.Price;
                offerPrice = linkedOfferPrice.Value;
                offerStart = null;
                offerEnd = null;
            }
        }

        return new ProductResponseDto(
            product.Id,
            product.Name,
            product.Description,
            displayPrice,
            product.StockQuantity,
            product.CategoryId,
            product.Category?.Name,
            product.Category?.Slug,
            orderedImages,
            sizesList,
            product.Slug,
            product.Sku,
            product.Barcode,
            product.Status,
            product.ShortDescription,
            compareAtPrice,
            offerPrice,
            offerStart,
            offerEnd,
            product.IsActive,
            product.IsFeatured,
            product.IsBestSeller,
            product.IsNewArrival,
            product.SeoTitle,
            product.SeoDescription,
            product.Variants
                .OrderBy(variant => variant.SortOrder)
                .Select(variant => new ProductVariantDto(
                    variant.Id,
                    variant.OptionKey,
                    variant.Sku,
                    variant.Barcode,
                    variant.ColorName,
                    variant.ColorSlug,
                    variant.SizeId,
                    variant.Size?.Name,
                    variant.StockQuantity,
                    variant.PriceOverride,
                    variant.IsActive,
                    variant.SortOrder))
                .ToList());
    }

    private void ApplyDisplayPricing(
        ProductReadDto item,
        DateTimeOffset currentTime,
        IReadOnlyList<OfferSnapshot> activeOffers)
    {
        var originalPrice = item.Price;
        var directOfferIsActive = IsDirectOfferActive(item.OfferPrice, item.OfferStart, item.OfferEnd, currentTime);

        if (directOfferIsActive && item.OfferPrice.HasValue)
        {
            item.Price = item.OfferPrice.Value;
            item.CompareAtPrice = item.CompareAtPrice is > 0m && item.CompareAtPrice > item.Price
                ? item.CompareAtPrice
                : originalPrice;
            item.IsOnOffer = true;
            return;
        }

        var linkedOfferPrice = ComputeLinkedOfferPrice(originalPrice, item.Id, item.CategoryId, activeOffers);
        if (linkedOfferPrice is not null && linkedOfferPrice.Value < originalPrice)
        {
            item.Price = linkedOfferPrice.Value;
            item.CompareAtPrice = item.CompareAtPrice is > 0m && item.CompareAtPrice > item.Price
                ? item.CompareAtPrice
                : originalPrice;
            item.OfferPrice = linkedOfferPrice.Value;
            item.OfferStart = null;
            item.OfferEnd = null;
            item.IsOnOffer = true;
            return;
        }

        item.IsOnOffer = false;
    }

    private static IQueryable<Product> ApplyCatalogSort(
        IQueryable<Product> productsQuery,
        string? sort,
        DateTimeOffset currentTime)
    {
        return sort?.Trim().ToLowerInvariant() switch
        {
            "price-low-to-high" or "priceasc" or "price_asc" => productsQuery
                .OrderBy(product => product.OfferPrice.HasValue
                    && (!product.OfferStart.HasValue || product.OfferStart <= currentTime)
                    && (!product.OfferEnd.HasValue || product.OfferEnd >= currentTime)
                        ? product.OfferPrice.Value
                        : product.Price)
                .ThenBy(product => product.Id),
            "price-high-to-low" or "pricedesc" or "price_desc" => productsQuery
                .OrderByDescending(product => product.OfferPrice.HasValue
                    && (!product.OfferStart.HasValue || product.OfferStart <= currentTime)
                    && (!product.OfferEnd.HasValue || product.OfferEnd >= currentTime)
                        ? product.OfferPrice.Value
                        : product.Price)
                .ThenBy(product => product.Id),
            "featured" => productsQuery
                .OrderByDescending(product => product.IsFeatured)
                .ThenByDescending(product => product.CreatedAt)
                .ThenByDescending(product => product.Id),
            "best-selling" or "bestselling" or "best_selling" => productsQuery
                .OrderByDescending(product => product.IsBestSeller)
                .ThenByDescending(product => product.CreatedAt)
                .ThenByDescending(product => product.Id),
            "newest" => productsQuery
                .OrderByDescending(product => product.CreatedAt)
                .ThenByDescending(product => product.Id),
            "oldest" => productsQuery
                .OrderBy(product => product.CreatedAt)
                .ThenBy(product => product.Id),
            _ => productsQuery
                .OrderByDescending(product => product.CreatedAt)
                .ThenByDescending(product => product.Id)
        };
    }

    private async Task EnsureCategoryExistsAsync(int categoryId, CancellationToken cancellationToken)
    {
        var exists = await _context.Categories.AnyAsync(category => category.Id == categoryId, cancellationToken);
        if (!exists)
        {
            throw new InvalidOperationException("Selected category does not exist.");
        }
    }

    private async Task EnsureSizesExistAsync(IEnumerable<int> sizeIds, CancellationToken cancellationToken)
    {
        var distinctSizeIds = sizeIds
            .Where(sizeId => sizeId > 0)
            .Distinct()
            .ToArray();

        if (distinctSizeIds.Length == 0)
        {
            return;
        }

        var count = await _context.Sizes.CountAsync(size => distinctSizeIds.Contains(size.Id), cancellationToken);
        if (count != distinctSizeIds.Length)
        {
            throw new InvalidOperationException("One or more selected sizes no longer exist.");
        }
    }

    private async Task<string> BuildUniqueSlugAsync(string? requestedSlug, string fallbackName, int? excludedProductId, CancellationToken cancellationToken)
    {
        var baseSlug = SlugUtility.GenerateSlug(requestedSlug) switch
        {
            { Length: > 0 } generatedSlug => generatedSlug,
            _ => SlugUtility.GenerateSlug(fallbackName)
        };

        if (string.IsNullOrWhiteSpace(baseSlug))
        {
            throw new InvalidOperationException("A valid product slug could not be generated.");
        }

        var slug = baseSlug;
        var suffix = 2;
        while (await _context.Products
                   .IgnoreQueryFilters()
                   .AnyAsync(product => product.Slug == slug && (!excludedProductId.HasValue || product.Id != excludedProductId.Value), cancellationToken))
        {
            slug = $"{baseSlug}-{suffix++}";
        }

        return slug;
    }

    private static void MapProductFields(Product product, UpsertProductDto dto)
    {
        product.Name = dto.Name.Trim();
        product.ShortDescription = NormalizeOptionalText(dto.ShortDescription);
        product.Description = dto.Description.Trim();
        product.CategoryId = dto.CategoryId;
        product.Price = dto.Price;
        product.CompareAtPrice = dto.CompareAtPrice;
        product.OfferPrice = dto.OfferPrice;
        product.OfferStart = dto.OfferStart;
        product.OfferEnd = dto.OfferEnd;
        product.StockQuantity = dto.StockQuantity;
        product.Status = string.IsNullOrWhiteSpace(dto.Status) ? "Draft" : dto.Status.Trim();
        product.IsActive = dto.IsActive;
        product.IsFeatured = dto.IsFeatured;
        product.IsBestSeller = dto.IsBestSeller;
        product.IsNewArrival = dto.IsNewArrival;
    }

    private void ApplySizesAndVariants(Product product, UpsertProductDto dto)
    {
        var selectedSizeIds = dto.SelectedSizeIds
            .Where(sizeId => sizeId > 0)
            .Distinct()
            .ToArray();
        var colors = dto.Colors
            .Select(color => color?.Trim())
            .Where(color => !string.IsNullOrWhiteSpace(color))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Cast<string>()
            .ToArray();

        product.ProductSizes.Clear();
        foreach (var sizeId in selectedSizeIds)
        {
            product.ProductSizes.Add(new ProductSize { ProductId = product.Id, SizeId = sizeId });
        }

        _context.ProductVariants.RemoveRange(product.Variants);
        product.Variants.Clear();

        if (colors.Length == 0)
        {
            return;
        }

        var sortOrder = 0;
        if (selectedSizeIds.Length == 0)
        {
            foreach (var color in colors)
            {
                product.Variants.Add(new ProductVariant
                {
                    ProductId = product.Id,
                    OptionKey = SlugUtility.GenerateSlug(color),
                    ColorName = color,
                    ColorSlug = SlugUtility.GenerateSlug(color),
                    StockQuantity = product.StockQuantity,
                    IsActive = product.IsActive,
                    SortOrder = sortOrder++
                });
            }

            return;
        }

        foreach (var color in colors)
        {
            var colorSlug = SlugUtility.GenerateSlug(color);
            foreach (var sizeId in selectedSizeIds)
            {
                product.Variants.Add(new ProductVariant
                {
                    ProductId = product.Id,
                    OptionKey = $"{colorSlug}:{sizeId}",
                    SizeId = sizeId,
                    ColorName = color,
                    ColorSlug = colorSlug,
                    StockQuantity = product.StockQuantity,
                    IsActive = product.IsActive,
                    SortOrder = sortOrder++
                });
            }
        }
    }

    private async Task<List<StoredImageFile>> SaveNewImagesAsync(
        UpsertProductDto dto,
        string filePrefix,
        CancellationToken cancellationToken)
    {
        var savedImages = new List<StoredImageFile>();

        foreach (var file in dto.Images)
        {
            savedImages.Add(await _imageStorage.SaveImageAsync(file, "products", filePrefix, cancellationToken));
        }

        return savedImages;
    }

    private HashSet<string> ApplyImages(
        Product product,
        UpsertProductDto dto,
        IReadOnlyList<string> existingImageUrls,
        IReadOnlyList<StoredImageFile> savedImages)
    {
        var existingTokens = product.Images
            .OrderBy(image => image.SortOrder)
            .ThenByDescending(image => image.IsMain)
            .ToDictionary(
                image => BuildExistingImageToken(image.ImageUrl),
                image => NormalizeImagePath(image.ImageUrl),
                StringComparer.OrdinalIgnoreCase);
        var newTokens = savedImages
            .Select((image, index) => new KeyValuePair<string, string>($"new:{index}", image.RelativePath))
            .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.OrdinalIgnoreCase);

        var orderedTokens = dto.ImageOrder
            .Where(token => !string.IsNullOrWhiteSpace(token))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (orderedTokens.Count == 0)
        {
            orderedTokens.AddRange(existingImageUrls.Select(BuildExistingImageToken));
            orderedTokens.AddRange(newTokens.Keys.OrderBy(token => token, StringComparer.OrdinalIgnoreCase));
        }

        var keptExistingUrls = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var finalImages = new List<ProductImage>();
        var mainToken = !string.IsNullOrWhiteSpace(dto.MainImageReference) ? dto.MainImageReference! : orderedTokens.FirstOrDefault();
        var sortOrder = 0;

        foreach (var token in orderedTokens)
        {
            string? resolvedUrl = null;
            if (existingTokens.TryGetValue(token, out var existingUrl))
            {
                resolvedUrl = existingUrl;
                keptExistingUrls.Add(existingUrl);
            }
            else if (newTokens.TryGetValue(token, out var newUrl))
            {
                resolvedUrl = newUrl;
            }

            if (string.IsNullOrWhiteSpace(resolvedUrl))
            {
                continue;
            }

            finalImages.Add(new ProductImage
            {
                ImageUrl = resolvedUrl,
                IsMain = string.Equals(token, mainToken, StringComparison.OrdinalIgnoreCase),
                SortOrder = sortOrder++
            });
        }

        if (finalImages.Count == 0)
        {
            throw new InvalidOperationException("Please keep or upload at least one product image.");
        }

        if (!finalImages.Any(image => image.IsMain))
        {
            finalImages[0].IsMain = true;
        }

        _context.ProductImages.RemoveRange(product.Images);
        product.Images.Clear();
        foreach (var image in finalImages)
        {
            product.Images.Add(image);
        }

        return keptExistingUrls;
    }

    private static void ValidateAdminProduct(UpsertProductDto dto, bool creating)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
        {
            throw new InvalidOperationException("Product name is required.");
        }

        if (string.IsNullOrWhiteSpace(dto.Description))
        {
            throw new InvalidOperationException("Product description is required.");
        }

        if (dto.Price <= 0)
        {
            throw new InvalidOperationException("Product price must be greater than zero.");
        }

        if (dto.StockQuantity < 0)
        {
            throw new InvalidOperationException("Stock quantity cannot be negative.");
        }

        if (dto.OfferEnd.HasValue && dto.OfferStart.HasValue && dto.OfferEnd <= dto.OfferStart)
        {
            throw new InvalidOperationException("Offer end must be after the offer start.");
        }

        if (creating && dto.Images.Count == 0)
        {
            throw new InvalidOperationException("Please upload at least one product image.");
        }
    }

    private static void CleanupSavedImages(IEnumerable<StoredImageFile> savedImages)
    {
        foreach (var image in savedImages)
        {
            if (File.Exists(image.AbsolutePath))
            {
                File.Delete(image.AbsolutePath);
            }
        }
    }

    private async Task<IReadOnlyList<OfferSnapshot>> GetActiveOfferSnapshotsAsync(DateTimeOffset now, CancellationToken cancellationToken)
    {
        var offers = await _context.Offers
            .AsNoTracking()
            .Where(offer => offer.IsActive && offer.StartDate <= now && offer.EndDate >= now)
            .Include(offer => offer.Products)
            .Include(offer => offer.Categories)
            .ToListAsync(cancellationToken);

        return offers.Select(offer => new OfferSnapshot(
            offer.DiscountType,
            offer.DiscountValue,
            offer.Products.Select(link => link.ProductId).ToHashSet(),
            offer.Categories.Select(link => link.CategoryId).ToHashSet()))
            .ToList();
    }

    private decimal? ComputeLinkedOfferPrice(
        decimal basePrice,
        int productId,
        int categoryId,
        IReadOnlyList<OfferSnapshot> offers)
    {
        decimal? bestPrice = null;

        foreach (var offer in offers)
        {
            if (!offer.ProductIds.Contains(productId) && !offer.CategoryIds.Contains(categoryId))
            {
                continue;
            }

            var discountedPrice = offer.DiscountType == OfferDiscountType.FixedAmount
                ? Math.Max(0m, basePrice - offer.DiscountValue)
                : Math.Max(0m, basePrice - (basePrice * offer.DiscountValue / 100m));

            bestPrice = bestPrice is null ? discountedPrice : Math.Min(bestPrice.Value, discountedPrice);
        }

        return bestPrice;
    }

    private static bool IsDirectOfferActive(
        decimal? offerPrice,
        DateTimeOffset? offerStart,
        DateTimeOffset? offerEnd,
        DateTimeOffset now)
    {
        return offerPrice.HasValue
            && (!offerStart.HasValue || offerStart <= now)
            && (!offerEnd.HasValue || offerEnd >= now);
    }

    private static string BuildExistingImageToken(string imageUrl)
    {
        return $"existing:{NormalizeImagePath(imageUrl)}";
    }

    private static string? NormalizeOptionalText(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static string NormalizeImagePath(string imageUrl)
    {
        if (string.IsNullOrWhiteSpace(imageUrl))
        {
            return string.Empty;
        }

        if (Uri.TryCreate(imageUrl, UriKind.Absolute, out _))
        {
            return imageUrl;
        }

        var normalizedPath = imageUrl.Replace('\\', '/').Trim();
        if (normalizedPath.StartsWith('/'))
        {
            return normalizedPath;
        }

        if (normalizedPath.StartsWith("catalog/", StringComparison.OrdinalIgnoreCase)
            || normalizedPath.StartsWith("images/", StringComparison.OrdinalIgnoreCase))
        {
            return $"/{normalizedPath.TrimStart('/')}";
        }

        return $"/images/{normalizedPath.TrimStart('/')}";
    }

    private sealed record OfferSnapshot(
        OfferDiscountType DiscountType,
        decimal DiscountValue,
        HashSet<int> ProductIds,
        HashSet<int> CategoryIds);
}
