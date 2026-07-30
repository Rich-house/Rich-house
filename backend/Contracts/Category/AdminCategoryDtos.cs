namespace Marketify.Contracts.Category;

public sealed record AdminCategoryListItemDto(
    int Id,
    string Name,
    string Slug,
    string? ImageUrl,
    bool IsActive,
    int DisplayOrder,
    int ProductCount);

public sealed record AdminCategoryDetailsDto(
    int Id,
    string Name,
    string Slug,
    string? ImageUrl,
    bool IsActive,
    int DisplayOrder);

public sealed class UpsertCategoryDto
{
    public string Name { get; set; } = string.Empty;
    public string? Slug { get; set; }
    public bool IsActive { get; set; } = true;
    public int DisplayOrder { get; set; }
    public bool RemoveImage { get; set; }
    public IFormFile? Image { get; set; }
}
