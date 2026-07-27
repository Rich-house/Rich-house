using Marketify.Contracts.Category;
using Marketify.Roles;
using Marketify.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Marketify.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CategoryController(ICategoryService categoryService) : ControllerBase
    {
        private readonly ICategoryService _categoryService = categoryService;

        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = $"{AppRoles.Admin},{AppRoles.SuperAdmin}")]
        [HttpPost]
        public async Task<IActionResult> AddCategory([FromBody] CreateCategoryDto categoryDto)
        {
            var result = await _categoryService.CreateCategory(categoryDto);
            return result ? Ok(result) : BadRequest();
        }

        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = $"{AppRoles.Admin},{AppRoles.SuperAdmin}")]
        [HttpPut("Edit/{id}")]
        public async Task<IActionResult> EditCategory([FromRoute] int id, [FromBody] EditCategory editCategory)
        {
            var result = await _categoryService.EditCategory(id, editCategory);
            return result ? Ok(result) : BadRequest();
        }

        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = $"{AppRoles.Admin},{AppRoles.SuperAdmin}")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> SoftDeleteCategory([FromRoute] int id)
        {
            var result = await _categoryService.SoftDelete(id);
            return result ? Ok(result) : BadRequest();
        }

        [HttpGet("GetById/{id}")]
        public async Task<IActionResult> GetCategoryById([FromRoute] int id)
        {
            var response = await _categoryService.GetCategoryById(id);
            return response is null ? NotFound() : Ok(response);
        }

        [HttpGet("slug/{slug}")]
        public async Task<IActionResult> GetCategoryBySlug([FromRoute] string slug)
        {
            var response = await _categoryService.GetCategoryBySlugAsync(slug);
            return response is null ? NotFound() : Ok(response);
        }

        [HttpGet]
        public async Task<IActionResult> GetAllCategories()
        {
            var result = await _categoryService.GetAllCategories();
            return Ok(result);
        }
    }
}
