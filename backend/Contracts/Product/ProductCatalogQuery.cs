namespace Marketify.Contracts.Product;

public sealed class ProductCatalogQuery
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 12;
    public string? Search { get; set; }
    public int? CategoryId { get; set; }
    public string? CategorySlug { get; set; }
    public string? Size { get; set; }
    public string? Color { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public bool? Featured { get; set; }
    public bool? BestSeller { get; set; }
    public bool? NewArrival { get; set; }
    public bool? OnOffer { get; set; }
    public bool? InStock { get; set; }
    public string? Sort { get; set; }
}
