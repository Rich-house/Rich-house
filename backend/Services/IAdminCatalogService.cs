using Marketify.Contracts.Admin;
using Marketify.Contracts.Common;

namespace Marketify.Services;

public interface IAdminCatalogService
{
    Task<AdminDashboardSummaryDto> GetDashboardSummaryAsync(CancellationToken cancellationToken = default);
    Task<PagedResult<AdminProductDiscountListItemDto>> GetProductDiscountsAsync(AdminProductDiscountQuery query, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AdminLookupItemDto>> GetSizeOptionsAsync(CancellationToken cancellationToken = default);
}
