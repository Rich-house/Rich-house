using Marketify.Contracts.Category;
using Marketify.Date;
using Marketify.Entites;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Marketify.Services
{
    public class CategoryService : ICategoryService
    {
        private const string AllCategoriesCacheKey = "all_categories_list";
        private readonly ApplicationDbContext _context;
        private readonly IMemoryCache _cache;

        public CategoryService(ApplicationDbContext context, IMemoryCache cache)
        {
            _context = context;
            _cache = cache;
        }
        public async Task<bool> CreateCategory(CreateCategoryDto Dto)
        {
            var category = new Category
            {
                Name = Dto.Name
            };

            _context.Categories.Add(category);
            var result = await _context.SaveChangesAsync();
            if (result > 0)
            {
                _cache.Remove(AllCategoriesCacheKey);
            }

            return result > 0;
        }

        public async Task<bool> EditCategory(int Id, EditCategory Dto)
        {
            var category = await _context.Categories.SingleOrDefaultAsync(x => x.Id == Id);
            if (category is null)
            {
                return false;
            }

            if (!string.Equals(Dto.Name, category.Name, StringComparison.Ordinal))
            {
                category.Name = Dto.Name;
            }

            var result = await _context.SaveChangesAsync();
            if (result > 0)
            {
                _cache.Remove(AllCategoriesCacheKey);
            }

            return result > 0;
        }

        public async Task<bool> SoftDelete(int Id)
        {
            var category = await _context.Categories.SingleOrDefaultAsync(x => x.Id == Id);
            if (category is null || category.IsDeleted)
            {
                return false;
            }

            category.IsDeleted = true;
            var result = await _context.SaveChangesAsync();
            if (result > 0)
            {
                _cache.Remove(AllCategoriesCacheKey);
            }

            return result > 0;
        }

        public async Task<GetCategoryByIdDTO?> GetCategoryById(int Id)
        {
            var category = await _context.Categories
                .AsNoTracking()
                .Where(category => category.IsActive)
                .SingleOrDefaultAsync(c => c.Id == Id);

            return category is null
                ? null
                : new GetCategoryByIdDTO(category.Id, category.Name ?? string.Empty, category.Slug, category.ImageUrl);
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
                : new GetCategoryByIdDTO(category.Id, category.Name ?? string.Empty, category.Slug, category.ImageUrl);
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
                    .Select(x => new GetCategoryByIdDTO(x.Id, x.Name!, x.Slug, x.ImageUrl))
                    .ToListAsync();

                var cacheOptions = new MemoryCacheEntryOptions()
                    .SetAbsoluteExpiration(TimeSpan.FromHours(1))
                    .SetPriority(CacheItemPriority.High);

                _cache.Set(AllCategoriesCacheKey, categories, cacheOptions);
            }

            return categories!;
        }
    }
}
