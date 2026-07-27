namespace Marketify.Contracts.Product
{
    public record EditProduct
    (string Name,
        string Description,
        decimal Price,
        int StockQuantity,
        int CategoryId,
        List<int>? SelectedSizeIds,
        List<IFormFile>? Images,
        int MainImageIndex





        );
}
