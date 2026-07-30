using Marketify.Contracts.Common;
using Marketify.Contracts.Product;

namespace Marketify.Services;

public interface IProductService
{
    Task<PagedResult<AdminProductListItemDto>> GetAdminProductsAsync(AdminProductQuery query, CancellationToken cancellationToken = default);
    Task<AdminProductDetailsDto?> GetAdminProductByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<AdminProductDetailsDto> CreateAdminProductAsync(UpsertProductDto dto, CancellationToken cancellationToken = default);
    Task<AdminProductDetailsDto> UpdateAdminProductAsync(int id, UpsertProductDto dto, CancellationToken cancellationToken = default);
    Task DeleteProductAsync(int id, CancellationToken cancellationToken = default);
    Task<ProductResponseDto?> GetByID(int id);
    Task<ProductResponseDto?> GetBySlugAsync(string slug);
    Task<PagedResult<ProductReadDto>> GetCatalogAsync(ProductCatalogQuery query);
    Task<IEnumerable<ProductReadDto>> GetAllProductsAsync();
    Task<IEnumerable<ProductReadDto>> GetProductByCategory(int? categoryId);
    Task<IEnumerable<ProductReadDto>> SearchProductsAsync(string searchTerm);
}
