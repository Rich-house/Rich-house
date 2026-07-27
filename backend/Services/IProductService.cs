using Marketify.Contracts.Common;
using Marketify.Contracts.Product;
using Marketify.Contracts.Review;

namespace Marketify.Services
{
    public interface IProductService
    {
        Task<bool> CreateProductAsync(CreateProduct dto);
        Task<bool> EditProductAsync(int Id,EditProduct dto);
        Task<bool> DeleteProductAsync(int Id);
        Task<ProductResponseDto?> GetByID(int Id);
        Task<ProductResponseDto?> GetBySlugAsync(string slug);
        Task<PagedResult<ProductReadDto>> GetCatalogAsync(ProductCatalogQuery query);
        public Task<IEnumerable<ProductReadDto>> GetAllProductsAsync();
        public Task<IEnumerable<ProductReadDto>> GetProductByCategory(int?  categoryId);
        public  Task<IEnumerable<ProductReadDto>> SearchProductsAsync(string searchTerm);
    }
}
