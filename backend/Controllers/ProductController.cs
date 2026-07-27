using Marketify.Contracts.Common;
using Marketify.Contracts.Product;
using Marketify.Roles;
using Marketify.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Marketify.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductController(IProductService productService) : ControllerBase
    {
        private readonly IProductService _productService = productService;

        [HttpPost]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = $"{AppRoles.Admin},{AppRoles.SuperAdmin}")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Create([FromForm] CreateProduct request)
        {
            if (request.Images is null || request.Images.Count == 0)
            {
                return BadRequest("Please upload at least one image.");
            }

            try
            {
                var result = await _productService.CreateProductAsync(request);
                return result ? Ok(new { message = "Product created successfully." }) : BadRequest("Failed to create product.");
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, $"Something went wrong: {ex.Message}");
            }
        }

        [HttpPut("{id:int}")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = $"{AppRoles.Admin},{AppRoles.SuperAdmin}")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UpdateProduct(int id, [FromForm] EditProduct dto)
        {
            if (id <= 0)
            {
                return BadRequest("Invalid product id.");
            }

            try
            {
                var product = await _productService.EditProductAsync(id, dto);
                return Ok(product);
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(ex.Message);
            }
        }

        [HttpDelete("{id:int}")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = $"{AppRoles.Admin},{AppRoles.SuperAdmin}")]
        public async Task<IActionResult> DeleteProduct([FromRoute] int id)
        {
            if (id <= 0)
            {
                return BadRequest("Invalid product id.");
            }

            try
            {
                var isDeleted = await _productService.DeleteProductAsync(id);
                return isDeleted ? Ok() : NotFound("Product not found in the database. It may have already been deleted.");
            }
            catch (Exception ex)
            {
                return BadRequest($"Failed to delete product. Error: {ex.InnerException?.Message ?? ex.Message}");
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
            var result = await _productService.GetCatalogAsync(query);
            return Ok(result);
        }

        [HttpGet("offers")]
        public async Task<ActionResult<PagedResult<ProductReadDto>>> GetOffers([FromQuery] ProductCatalogQuery query)
        {
            query.OnOffer = true;
            query.Sort ??= "featured";
            var result = await _productService.GetCatalogAsync(query);
            return Ok(result);
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ProductReadDto>>> GetProducts()
        {
            var products = await _productService.GetAllProductsAsync();
            return Ok(products);
        }

        [HttpGet("GetProductsByCatID/{id:int}")]
        public async Task<ActionResult<IEnumerable<ProductReadDto>>> GetAllProductByCateId([FromRoute] int id)
        {
            var products = await _productService.GetProductByCategory(id);
            return Ok(products);
        }

        [HttpGet("search")]
        public async Task<IActionResult> Search([FromQuery] string query)
        {
            var results = await _productService.SearchProductsAsync(query);
            return Ok(results);
        }
    }
}
