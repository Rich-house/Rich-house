using Marketify.Contracts.Category;
using Marketify.Roles;
using Marketify.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Marketify.Controllers;

[Route("api/[controller]")]
[ApiController]
public class CategoryController(ICategoryService categoryService) : ControllerBase
{
    private readonly ICategoryService _categoryService = categoryService;

    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = $"{AppRoles.Admin},{AppRoles.SuperAdmin}")]
    [HttpGet("admin")]
    public async Task<ActionResult<IReadOnlyList<AdminCategoryListItemDto>>> GetAdminCategories(CancellationToken cancellationToken)
    {
        return Ok(await _categoryService.GetAdminCategoriesAsync(cancellationToken));
    }

    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = $"{AppRoles.Admin},{AppRoles.SuperAdmin}")]
    [HttpGet("admin/{id:int}")]
    public async Task<ActionResult<AdminCategoryDetailsDto>> GetAdminCategoryById(int id, CancellationToken cancellationToken)
    {
        var category = await _categoryService.GetAdminCategoryByIdAsync(id, cancellationToken);
        return category is null ? NotFound() : Ok(category);
    }

    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = $"{AppRoles.Admin},{AppRoles.SuperAdmin}")]
    [HttpPost("admin")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<AdminCategoryDetailsDto>> CreateCategory([FromForm] UpsertCategoryDto dto, CancellationToken cancellationToken)
    {
        try
        {
            var category = await _categoryService.CreateAdminCategoryAsync(dto, cancellationToken);
            return Ok(category);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = $"{AppRoles.Admin},{AppRoles.SuperAdmin}")]
    [HttpPut("admin/{id:int}")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<AdminCategoryDetailsDto>> UpdateCategory(int id, [FromForm] UpsertCategoryDto dto, CancellationToken cancellationToken)
    {
        try
        {
            var category = await _categoryService.UpdateAdminCategoryAsync(id, dto, cancellationToken);
            return Ok(category);
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

    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = $"{AppRoles.Admin},{AppRoles.SuperAdmin}")]
    [HttpDelete("admin/{id:int}")]
    public async Task<IActionResult> DeleteCategory(int id, CancellationToken cancellationToken)
    {
        try
        {
            await _categoryService.DeleteCategoryAsync(id, cancellationToken);
            return NoContent();
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not found", StringComparison.OrdinalIgnoreCase))
        {
            return NotFound(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
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
        return Ok(await _categoryService.GetAllCategories());
    }
}
