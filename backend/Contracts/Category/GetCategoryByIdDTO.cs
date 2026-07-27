namespace Marketify.Contracts.Category
{
    public record GetCategoryByIdDTO
    (
        int Id ,
        string Name,
        string? Slug,
        string? ImageUrl
        
        )   ;
    
}
