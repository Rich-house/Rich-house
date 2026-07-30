using Marketify.Contracts.Common;
using Marketify.Contracts.Product;
using Marketify.Roles;
using Marketify.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Marketify.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ProductController(IProductService productService) : ControllerBase
{
    private readonly IProductService _productService = productService;

    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = $"{AppRoles.Admin},{AppRoles.SuperAdmin}")]
    [HttpGet("admin")]
    public async Task<ActionResult<PagedResult<AdminProductListItemDto>>> GetAdminProducts([FromQuery] AdminProductQuery query, CancellationToken cancellationToken)
    {
        return Ok(await _productService.GetAdminProductsAsync(query, cancellationToken));
    }

    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = $"{AppRoles.Admin},{AppRoles.SuperAdmin}")]
    [HttpGet("admin/{id:int}")]
    public async Task<ActionResult<AdminProductDetailsDto>> GetAdminProductById(int id, CancellationToken cancellationToken)
    {
        var product = await _productService.GetAdminProductByIdAsync(id, cancellationToken);
        return product is null ? NotFound() : Ok(product);
    }

    [HttpPost]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = $"{AppRoles.Admin},{AppRoles.SuperAdmin}")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Create([FromForm] UpsertProductDto request, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _productService.CreateAdminProductAsync(request, cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPut("{id:int}")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = $"{AppRoles.Admin},{AppRoles.SuperAdmin}")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UpdateProduct(int id, [FromForm] UpsertProductDto dto, CancellationToken cancellationToken)
    {
        if (id <= 0)
        {
            return BadRequest("Invalid product id.");
        }

        try
        {
            return Ok(await _productService.UpdateAdminProductAsync(id, dto, cancellationToken));
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not found", StringComparison.OrdinalIgnoreCase))
        {
            return NotFound(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpDelete("{id:int}")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = $"{AppRoles.Admin},{AppRoles.SuperAdmin}")]
    public async Task<IActionResult> DeleteProduct([FromRoute] int id, CancellationToken cancellationToken)
    {
        if (id <= 0)
        {
            return BadRequest("Invalid product id.");
        }

        try
        {
            await _productService.DeleteProductAsync(id, cancellationToken);
            return NoContent();
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not found", StringComparison.OrdinalIgnoreCase))
        {
            return NotFound(ex.Message);
        }
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById([FromRoute] int id)
    {
        var productDto = await _productService.GetByID(id);
        return productDto is null ? NotFound() : Ok(productDto);
    }

    [HttpGet("slug/{slug}")]
    public async Task<IActionResult> GetBySlug([FromRoute] string slug)
    {
        var productDto = await _productService.GetBySlugAsync(slug);
        return productDto is null ? NotFound() : Ok(productDto);
    }

    [HttpGet("catalog")]
    public async Task<ActionResult<PagedResult<ProductReadDto>>> GetCatalog([FromQuery] ProductCatalogQuery query)
    {
        return Ok(await _productService.GetCatalogAsync(query));
    }

    [HttpGet("offers")]
    public async Task<ActionResult<PagedResult<ProductReadDto>>> GetOffers([FromQuery] ProductCatalogQuery query)
    {
        query.OnOffer = true;
        query.Sort ??= "featured";
        return Ok(await _productService.GetCatalogAsync(query));
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ProductReadDto>>> GetProducts()
    {
        return Ok(await _productService.GetAllProductsAsync());
    }

    [HttpGet("GetProductsByCatID/{id:int}")]
    public async Task<ActionResult<IEnumerable<ProductReadDto>>> GetAllProductByCateId([FromRoute] int id)
    {
        return Ok(await _productService.GetProductByCategory(id));
    }

    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string query)
    {
        return Ok(await _productService.SearchProductsAsync(query));
    }
}
