using Marketify.Contracts.Category;

namespace Marketify.Services
{
    public interface ICategoryService
    {
        Task<bool> CreateCategory(CreateCategoryDto Dto);
        Task<bool> EditCategory(int Id,EditCategory Dto);
        Task<bool> SoftDelete(int Id);
        Task<GetCategoryByIdDTO?> GetCategoryById(int Id);
        Task<GetCategoryByIdDTO?> GetCategoryBySlugAsync(string slug);
        Task<IEnumerable<GetCategoryByIdDTO>> GetAllCategories();
    }
}
