using Marketify.Contracts.Common;
using Marketify.Contracts.Offer;

namespace Marketify.Services;

public interface IOfferService
{
    Task<PagedResult<AdminOfferListItemDto>> GetAdminOffersAsync(AdminOfferQuery query, CancellationToken cancellationToken = default);
    Task<AdminOfferDetailsDto?> GetAdminOfferByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<AdminOfferDetailsDto> CreateOfferAsync(UpsertOfferDto dto, CancellationToken cancellationToken = default);
    Task<AdminOfferDetailsDto> UpdateOfferAsync(int id, UpsertOfferDto dto, CancellationToken cancellationToken = default);
    Task DeleteOfferAsync(int id, CancellationToken cancellationToken = default);
    Task ToggleOfferAsync(int id, bool isActive, CancellationToken cancellationToken = default);
}
