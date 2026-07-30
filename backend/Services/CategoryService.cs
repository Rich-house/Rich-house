using Marketify.Contracts.Category;
using Marketify.Date;
using Marketify.Entites;
using Marketify.Helper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Marketify.Services;

public class CategoryService(
    ApplicationDbContext context,
    IMemoryCache cache,
    CatalogImageStorageService imageStorage) : ICategoryService
{
    private const string AllCategoriesCacheKey = "all_categories_list";
    private readonly ApplicationDbContext _context = context;
    private readonly IMemoryCache _cache = cache;
    private readonly CatalogImageStorageService _imageStorage = imageStorage;

    public async Task<IReadOnlyList<AdminCategoryListItemDto>> GetAdminCategoriesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Categories
            .AsNoTracking()
            .OrderBy(category => category.DisplayOrder)
            .ThenBy(category => category.Name)
            .Select(category => new AdminCategoryListItemDto(
                category.Id,
                category.Name,
                category.Slug ?? string.Empty,
                category.ImageUrl,
                category.IsActive,
                category.DisplayOrder,
                category.Products.Count()))
            .ToListAsync(cancellationToken);
    }

    public async Task<AdminCategoryDetailsDto?> GetAdminCategoryByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Categories
            .AsNoTracking()
            .Where(category => category.Id == id)
            .Select(category => new AdminCategoryDetailsDto(
                category.Id,
                category.Name,
                category.Slug ?? string.Empty,
                category.ImageUrl,
                category.IsActive,
                category.DisplayOrder))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<AdminCategoryDetailsDto> CreateAdminCategoryAsync(UpsertCategoryDto dto, CancellationToken cancellationToken = default)
    {
        ValidateCategoryInput(dto);

        var category = new Category
        {
            Name = dto.Name.Trim(),
            IsActive = dto.IsActive,
            DisplayOrder = dto.DisplayOrder
        };

        category.Slug = await BuildUniqueSlugAsync(dto.Slug, category.Name, null, cancellationToken);

        StoredImageFile? newImage = null;
        try
        {
            if (dto.Image is not null)
            {
                newImage = await _imageStorage.SaveImageAsync(dto.Image, "categories", category.Slug, cancellationToken);
                category.ImageUrl = newImage.Value.RelativePath;
            }

            _context.Categories.Add(category);
            await _context.SaveChangesAsync(cancellationToken);
            InvalidatePublicCategoriesCache();

            return new AdminCategoryDetailsDto(
                category.Id,
                category.Name,
                category.Slug,
                category.ImageUrl,
                category.IsActive,
                category.DisplayOrder);
        }
        catch
        {
            if (newImage is not null)
            {
                _imageStorage.DeleteIfExists(newImage.Value.RelativePath);
            }

            throw;
        }
    }

    public async Task<AdminCategoryDetailsDto> UpdateAdminCategoryAsync(int id, UpsertCategoryDto dto, CancellationToken cancellationToken = default)
    {
        ValidateCategoryInput(dto);

        var category = await _context.Categories.SingleOrDefaultAsync(category => category.Id == id, cancellationToken);
        if (category is null)
        {
            throw new InvalidOperationException("Category not found.");
        }

        category.Name = dto.Name.Trim();
        category.IsActive = dto.IsActive;
        category.DisplayOrder = dto.DisplayOrder;
        category.Slug = await BuildUniqueSlugAsync(dto.Slug, category.Name, id, cancellationToken);

        var previousImageUrl = category.ImageUrl;
        StoredImageFile? newImage = null;

        try
        {
            if (dto.RemoveImage)
            {
                category.ImageUrl = null;
            }

            if (dto.Image is not null)
            {
                newImage = await _imageStorage.SaveImageAsync(dto.Image, "categories", category.Slug, cancellationToken);
                category.ImageUrl = newImage.Value.RelativePath;
            }

            await _context.SaveChangesAsync(cancellationToken);
            InvalidatePublicCategoriesCache();

            if (!string.Equals(previousImageUrl, category.ImageUrl, StringComparison.OrdinalIgnoreCase)
                && !string.IsNullOrWhiteSpace(previousImageUrl))
            {
                _imageStorage.DeleteIfExists(previousImageUrl);
            }

            return new AdminCategoryDetailsDto(
                category.Id,
                category.Name,
                category.Slug ?? string.Empty,
                category.ImageUrl,
                category.IsActive,
                category.DisplayOrder);
        }
        catch
        {
            if (newImage is not null)
            {
                _imageStorage.DeleteIfExists(newImage.Value.RelativePath);
            }

            category.ImageUrl = previousImageUrl;
            throw;
        }
    }

    public async Task DeleteCategoryAsync(int id, CancellationToken cancellationToken = default)
    {
        var category = await _context.Categories.SingleOrDefaultAsync(category => category.Id == id, cancellationToken);
        if (category is null || category.IsDeleted)
        {
            throw new InvalidOperationException("Category not found.");
        }

        var hasProducts = await _context.Products
            .IgnoreQueryFilters()
            .AnyAsync(product => product.CategoryId == id, cancellationToken);
        if (hasProducts)
        {
            throw new InvalidOperationException("This category cannot be deleted while products still belong to it.");
        }

        var hasOfferLinks = await _context.OfferCategories.AnyAsync(link => link.CategoryId == id, cancellationToken);
        if (hasOfferLinks)
        {
            throw new InvalidOperationException("This category cannot be deleted while offers are linked to it.");
        }

        category.IsDeleted = true;
        category.IsActive = false;
        await _context.SaveChangesAsync(cancellationToken);
        InvalidatePublicCategoriesCache();
    }

    public async Task<GetCategoryByIdDTO?> GetCategoryById(int id)
    {
        var category = await _context.Categories
            .AsNoTracking()
            .Where(category => category.IsActive)
            .SingleOrDefaultAsync(category => category.Id == id);

        return category is null
            ? null
            : new GetCategoryByIdDTO(category.Id, category.Name, category.Slug, category.ImageUrl);
    }

    public async Task<GetCategoryByIdDTO?> GetCategoryBySlugAsync(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug))
        {
            return null;
        }

        var normalizedSlug = slug.Trim();
        var category = await _context.Categories
            .AsNoTracking()
            .Where(category => category.IsActive)
            .SingleOrDefaultAsync(category => category.Slug == normalizedSlug);

        return category is null
            ? null
            : new GetCategoryByIdDTO(category.Id, category.Name, category.Slug, category.ImageUrl);
    }

    public async Task<IEnumerable<GetCategoryByIdDTO>> GetAllCategories()
    {
        if (!_cache.TryGetValue(AllCategoriesCacheKey, out IEnumerable<GetCategoryByIdDTO>? categories))
        {
            categories = await _context.Categories
                .AsNoTracking()
                .Where(category => category.IsActive)
                .OrderBy(category => category.DisplayOrder)
                .ThenBy(category => category.Name)
                .Select(category => new GetCategoryByIdDTO(
                    category.Id,
                    category.Name,
                    category.Slug,
                    category.ImageUrl))
                .ToListAsync();

            var cacheOptions = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromHours(1))
                .SetPriority(CacheItemPriority.High);

            _cache.Set(AllCategoriesCacheKey, categories, cacheOptions);
        }

        return categories!;
    }

    private async Task<string> BuildUniqueSlugAsync(
        string? requestedSlug,
        string fallbackName,
        int? excludedCategoryId,
        CancellationToken cancellationToken)
    {
        var baseSlug = SlugUtility.GenerateSlug(requestedSlug) switch
        {
            { Length: > 0 } generatedSlug => generatedSlug,
            _ => SlugUtility.GenerateSlug(fallbackName)
        };

        if (string.IsNullOrWhiteSpace(baseSlug))
        {
            throw new InvalidOperationException("A valid category slug could not be generated.");
        }

        var slug = baseSlug;
        var suffix = 2;

        while (await _context.Categories.AnyAsync(
                   category => category.Slug == slug
                       && (!excludedCategoryId.HasValue || category.Id != excludedCategoryId.Value),
                   cancellationToken))
        {
            slug = $"{baseSlug}-{suffix++}";
        }

        return slug;
    }

    private static void ValidateCategoryInput(UpsertCategoryDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
        {
            throw new InvalidOperationException("Category name is required.");
        }
    }

    private void InvalidatePublicCategoriesCache()
    {
        _cache.Remove(AllCategoriesCacheKey);
    }
}
