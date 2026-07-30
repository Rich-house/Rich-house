using Marketify.Contracts.Common;
using Marketify.Contracts.Offer;
using Marketify.Roles;
using Marketify.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Marketify.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = $"{AppRoles.Admin},{AppRoles.SuperAdmin}")]
public class OfferController(IOfferService offerService) : ControllerBase
{
    private readonly IOfferService _offerService = offerService;

    [HttpGet]
    public async Task<ActionResult<PagedResult<AdminOfferListItemDto>>> GetOffers([FromQuery] AdminOfferQuery query, CancellationToken cancellationToken)
    {
        return Ok(await _offerService.GetAdminOffersAsync(query, cancellationToken));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<AdminOfferDetailsDto>> GetOfferById(int id, CancellationToken cancellationToken)
    {
        var offer = await _offerService.GetAdminOfferByIdAsync(id, cancellationToken);
        return offer is null ? NotFound() : Ok(offer);
    }

    [HttpPost]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<AdminOfferDetailsDto>> CreateOffer([FromForm] UpsertOfferDto dto, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _offerService.CreateOfferAsync(dto, cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPut("{id:int}")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<AdminOfferDetailsDto>> UpdateOffer(int id, [FromForm] UpsertOfferDto dto, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _offerService.UpdateOfferAsync(id, dto, cancellationToken));
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

    [HttpPatch("{id:int}/activation")]
    public async Task<IActionResult> ToggleActivation(int id, [FromQuery] bool isActive, CancellationToken cancellationToken)
    {
        try
        {
            await _offerService.ToggleOfferAsync(id, isActive, cancellationToken);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(ex.Message);
        }
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteOffer(int id, CancellationToken cancellationToken)
    {
        try
        {
            await _offerService.DeleteOfferAsync(id, cancellationToken);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(ex.Message);
        }
    }
}
