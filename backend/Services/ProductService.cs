using Marketify.Contracts.Common;
using Marketify.Contracts.Product;
using Marketify.Date;
using Marketify.Entites;
using Microsoft.EntityFrameworkCore;

namespace Marketify.Services
{
    public class ProductService(ApplicationDbContext context, IWebHostEnvironment env) : IProductService
    {
        private readonly ApplicationDbContext _context = context;
        private readonly IWebHostEnvironment _env = env;

        public async Task<bool> CreateProductAsync(CreateProduct dto)
        {
            var product = new Product
            {
                Name = dto.Name,
                Description = dto.Description,
                Price = dto.Price,
                StockQuantity = dto.StockQuantity,
                CategoryId = dto.CategoryId,
                Images = [],
                ProductSizes = dto.SelectedSizeIds
                    .Select(id => new ProductSize { SizeId = id })
                    .ToList()
            };

            await AddUploadedImagesAsync(product, dto.Images, dto.MainImageIndex);

            _context.Products.Add(product);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> EditProductAsync(int id, EditProduct dto)
        {
            var product = await _context.Products
                .Include(p => p.Images)
                .Include(p => p.ProductSizes)
                .SingleOrDefaultAsync(x => x.Id == id);

            if (product is null)
            {
                throw new InvalidOperationException("Product not found.");
            }

            product.Name = dto.Name;
            product.Description = dto.Description;
            product.Price = dto.Price;
            product.StockQuantity = dto.StockQuantity;
            product.CategoryId = dto.CategoryId;

            if (dto.SelectedSizeIds is not null)
            {
                product.ProductSizes.Clear();

                foreach (var sizeId in dto.SelectedSizeIds.Distinct())
                {
                    product.ProductSizes.Add(new ProductSize { ProductId = product.Id, SizeId = sizeId });
                }
            }

            if (dto.Images is { Count: > 0 })
            {
                DeleteImagesFromDisk(product.Images.Select(image => image.ImageUrl));
                product.Images.Clear();
                await AddUploadedImagesAsync(product, dto.Images, dto.MainImageIndex);
            }

            var result = await _context.SaveChangesAsync();
            return result > 0;
        }

        public async Task<bool> DeleteProductAsync(int id)
        {
            var product = await _context.Products
                .Include(x => x.Images)
                .Include(p => p.ProductSizes)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (product is null)
            {
                return false;
            }

            DeleteImagesFromDisk(product.Images.Select(image => image.ImageUrl));
            _context.Products.Remove(product);
            var result = await _context.SaveChangesAsync();
            return result > 0;
        }

        public async Task<ProductResponseDto?> GetByID(int id)
        {
            var product = await _context.Products
                .AsNoTracking()
                .Where(product => product.IsActive && product.Category.IsActive)
                .Include(product => product.Category)
                .Include(p => p.Images)
                .Include(p => p.ProductSizes)
                    .ThenInclude(productSize => productSize.Size)
                .Include(p => p.Variants)
                    .ThenInclude(variant => variant.Size)
                .AsSplitQuery()
                .SingleOrDefaultAsync(p => p.Id == id);

            if (product is null)
            {
                return null;
            }

            return MapProductDetails(product);
        }

        public async Task<ProductResponseDto?> GetBySlugAsync(string slug)
        {
            if (string.IsNullOrWhiteSpace(slug))
            {
                return null;
            }

            var normalizedSlug = slug.Trim();
            var product = await _context.Products
                .AsNoTracking()
                .Where(product => product.IsActive && product.Category.IsActive)
                .Include(product => product.Category)
                .Include(p => p.Images)
                .Include(p => p.ProductSizes)
                    .ThenInclude(productSize => productSize.Size)
                .Include(p => p.Variants)
                    .ThenInclude(variant => variant.Size)
                .AsSplitQuery()
                .SingleOrDefaultAsync(product => product.Slug == normalizedSlug);

            return product is null ? null : MapProductDetails(product);
        }

        public async Task<PagedResult<ProductReadDto>> GetCatalogAsync(ProductCatalogQuery query)
        {
            var currentTime = DateTimeOffset.UtcNow;
            var page = Math.Max(query.Page, 1);
            var pageSize = Math.Clamp(query.PageSize, 1, 48);

            var productsQuery = BuildCatalogQuery(query, currentTime);
            var totalItems = await productsQuery.CountAsync();
            var sortedQuery = ApplyCatalogSort(productsQuery, query.Sort, currentTime);
            var pagedQuery = sortedQuery
                .Skip((page - 1) * pageSize)
                .Take(pageSize);
            var items = await ProjectCatalogItemsAsync(pagedQuery, currentTime);

            var totalPages = totalItems == 0
                ? 0
                : (int)Math.Ceiling(totalItems / (double)pageSize);

            return new PagedResult<ProductReadDto>(
                items,
                new PaginationMeta(
                    page,
                    pageSize,
                    totalItems,
                    totalPages,
                    page > 1,
                    totalPages > 0 && page < totalPages));
        }

        public async Task<IEnumerable<ProductReadDto>> GetAllProductsAsync()
        {
            var currentTime = DateTimeOffset.UtcNow;
            var productsQuery = ApplyCatalogSort(
                BuildCatalogQuery(new ProductCatalogQuery(), currentTime),
                "newest",
                currentTime);

            return await ProjectCatalogItemsAsync(productsQuery, currentTime);
        }

        public async Task<IEnumerable<ProductReadDto>> GetProductByCategory(int? categoryId)
        {
            if (categoryId is null)
            {
                return [];
            }

            var currentTime = DateTimeOffset.UtcNow;
            var productsQuery = ApplyCatalogSort(
                BuildCatalogQuery(new ProductCatalogQuery
                {
                    CategoryId = categoryId
                }, currentTime),
                "newest",
                currentTime);

            return await ProjectCatalogItemsAsync(productsQuery, currentTime);
        }

        public async Task<IEnumerable<ProductReadDto>> SearchProductsAsync(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                return [];
            }

            var currentTime = DateTimeOffset.UtcNow;
            var productsQuery = ApplyCatalogSort(
                BuildCatalogQuery(new ProductCatalogQuery
                {
                    Search = searchTerm
                }, currentTime),
                "newest",
                currentTime);

            return await ProjectCatalogItemsAsync(productsQuery, currentTime);
        }

        private async Task AddUploadedImagesAsync(Product product, IReadOnlyList<IFormFile> files, int mainImageIndex)
        {
            var imagesDirectory = EnsureImagesDirectory();

            for (var index = 0; index < files.Count; index++)
            {
                var formFile = files[index];
                if (formFile.Length <= 0)
                {
                    continue;
                }

                var fileName = $"{Guid.NewGuid()}{Path.GetExtension(formFile.FileName)}";
                var path = Path.Combine(imagesDirectory, fileName);

                await using var stream = new FileStream(path, FileMode.Create);
                await formFile.CopyToAsync(stream);

                product.Images.Add(new ProductImage
                {
                    ImageUrl = $"/images/{fileName}",
                    IsMain = index == mainImageIndex
                });
            }
        }

        private string EnsureImagesDirectory()
        {
            var webRootPath = _env.WebRootPath;
            if (string.IsNullOrWhiteSpace(webRootPath))
            {
                webRootPath = Path.Combine(_env.ContentRootPath, "wwwroot");
            }

            var imagesDirectory = Path.Combine(webRootPath, "images");
            Directory.CreateDirectory(imagesDirectory);
            return imagesDirectory;
        }

        private IQueryable<Product> BuildCatalogQuery(ProductCatalogQuery query, DateTimeOffset currentTime)
        {
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
                        variant.IsActive
                        && (variant.ColorSlug == color || variant.ColorName == color)));
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
                        product.OfferPrice.HasValue
                        && (!product.OfferStart.HasValue || product.OfferStart <= currentTime)
                        && (!product.OfferEnd.HasValue || product.OfferEnd >= currentTime))
                    : productsQuery.Where(product =>
                        !product.OfferPrice.HasValue
                        || (product.OfferStart.HasValue && product.OfferStart > currentTime)
                        || (product.OfferEnd.HasValue && product.OfferEnd < currentTime));
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
            DateTimeOffset currentTime)
        {
            return await productsQuery
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
                    IsOnOffer = product.OfferPrice.HasValue
                        && (!product.OfferStart.HasValue || product.OfferStart <= currentTime)
                        && (!product.OfferEnd.HasValue || product.OfferEnd >= currentTime),
                    CategoryName = product.Category != null ? product.Category.Name ?? "General" : "General",
                    CategorySlug = product.Category != null ? product.Category.Slug : null,
                    ImageUrls = product.Images
                        .OrderBy(image => image.SortOrder)
                        .Select(image => NormalizeImagePath(image.ImageUrl))
                        .ToList(),
                    Sizes = product.ProductSizes
                        .OrderBy(productSize => productSize.Size!.SortOrder)
                        .Select(productSize => productSize.Size!.Name)
                        .ToList()
                })
                .ToListAsync();
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

        private ProductResponseDto MapProductDetails(Product product)
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

            return new ProductResponseDto(
                product.Id,
                product.Name,
                product.Description,
                product.Price,
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
                product.CompareAtPrice,
                product.OfferPrice,
                product.OfferStart,
                product.OfferEnd,
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

        private void DeleteImagesFromDisk(IEnumerable<string> imageUrls)
        {
            foreach (var imageUrl in imageUrls)
            {
                var path = ResolveImageFilePath(imageUrl);
                if (path is not null && File.Exists(path))
                {
                    File.Delete(path);
                }
            }
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

            var normalizedPath = imageUrl
                .Replace('\\', '/')
                .Trim();

            if (normalizedPath.StartsWith('/'))
            {
                return normalizedPath;
            }

            if (normalizedPath.StartsWith("catalog/", StringComparison.OrdinalIgnoreCase)
                || normalizedPath.StartsWith("images/", StringComparison.OrdinalIgnoreCase))
            {
                return $"/{normalizedPath.TrimStart('/')}";
            }

            var fileName = GetImageFileName(normalizedPath);
            return string.IsNullOrWhiteSpace(fileName) ? string.Empty : $"/images/{fileName}";
        }

        private static string GetImageFileName(string imageUrl)
        {
            return imageUrl
                .Replace("/images/", string.Empty, StringComparison.OrdinalIgnoreCase)
                .Replace("images/", string.Empty, StringComparison.OrdinalIgnoreCase)
                .TrimStart('/');
        }

        private string? ResolveImageFilePath(string imageUrl)
        {
            if (string.IsNullOrWhiteSpace(imageUrl) || Uri.TryCreate(imageUrl, UriKind.Absolute, out _))
            {
                return null;
            }

            var webRootPath = _env.WebRootPath;
            if (string.IsNullOrWhiteSpace(webRootPath))
            {
                webRootPath = Path.Combine(_env.ContentRootPath, "wwwroot");
            }

            var normalizedPath = imageUrl.Replace('\\', '/').Trim().TrimStart('/');
            if (normalizedPath.StartsWith("catalog/", StringComparison.OrdinalIgnoreCase)
                || normalizedPath.StartsWith("images/", StringComparison.OrdinalIgnoreCase))
            {
                return Path.Combine(webRootPath, normalizedPath.Replace('/', Path.DirectorySeparatorChar));
            }

            var fileName = GetImageFileName(normalizedPath);
            return string.IsNullOrWhiteSpace(fileName)
                ? null
                : Path.Combine(webRootPath, "images", fileName);
        }
    }
}
