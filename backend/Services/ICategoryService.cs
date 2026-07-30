using Marketify.Contracts.Category;

namespace Marketify.Services;

public interface ICategoryService
{
    Task<IReadOnlyList<AdminCategoryListItemDto>> GetAdminCategoriesAsync(CancellationToken cancellationToken = default);
    Task<AdminCategoryDetailsDto?> GetAdminCategoryByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<AdminCategoryDetailsDto> CreateAdminCategoryAsync(UpsertCategoryDto dto, CancellationToken cancellationToken = default);
    Task<AdminCategoryDetailsDto> UpdateAdminCategoryAsync(int id, UpsertCategoryDto dto, CancellationToken cancellationToken = default);
    Task DeleteCategoryAsync(int id, CancellationToken cancellationToken = default);
    Task<GetCategoryByIdDTO?> GetCategoryById(int id);
    Task<GetCategoryByIdDTO?> GetCategoryBySlugAsync(string slug);
    Task<IEnumerable<GetCategoryByIdDTO>> GetAllCategories();
}
