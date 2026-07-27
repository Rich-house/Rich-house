using Marketify.Contracts.ProductImage;

namespace Marketify.Contracts.Product
{
    public record CreateProduct(
        string Name,
        string Description,
        decimal Price,
        int StockQuantity,
        int CategoryId,
        List<int> SelectedSizeIds,
        List<IFormFile> Images,
        int MainImageIndex 
         
    );
}
    