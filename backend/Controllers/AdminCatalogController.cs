using Marketify.Contracts.Admin;
using Marketify.Contracts.Common;
using Marketify.Roles;
using Marketify.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Marketify.Controllers;

[Route("api/admin/catalog")]
[ApiController]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = $"{AppRoles.Admin},{AppRoles.SuperAdmin}")]
public class AdminCatalogController(IAdminCatalogService adminCatalogService) : ControllerBase
{
    private readonly IAdminCatalogService _adminCatalogService = adminCatalogService;

    [HttpGet("summary")]
    public async Task<ActionResult<AdminDashboardSummaryDto>> GetSummary(CancellationToken cancellationToken)
    {
        return Ok(await _adminCatalogService.GetDashboardSummaryAsync(cancellationToken));
    }

    [HttpGet("product-discounts")]
    public async Task<ActionResult<PagedResult<AdminProductDiscountListItemDto>>> GetProductDiscounts(
        [FromQuery] AdminProductDiscountQuery query,
        CancellationToken cancellationToken)
    {
        return Ok(await _adminCatalogService.GetProductDiscountsAsync(query, cancellationToken));
    }

    [HttpGet("sizes")]
    public async Task<ActionResult<IReadOnlyList<AdminLookupItemDto>>> GetSizes(CancellationToken cancellationToken)
    {
        return Ok(await _adminCatalogService.GetSizeOptionsAsync(cancellationToken));
    }
}
