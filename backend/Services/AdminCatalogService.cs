using Marketify.Contracts.Admin;
using Marketify.Contracts.Common;
using Marketify.Date;
using Marketify.Entites;
using Microsoft.EntityFrameworkCore;

namespace Marketify.Services;

public sealed class AdminCatalogService(ApplicationDbContext context) : IAdminCatalogService
{
    private readonly ApplicationDbContext _context = context;

    public async Task<AdminDashboardSummaryDto> GetDashboardSummaryAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var productQuery = _context.Products.IgnoreQueryFilters().AsNoTracking();
        var categoryQuery = _context.Categories.IgnoreQueryFilters().AsNoTracking().Where(category => !category.IsDeleted);
        var offerQuery = _context.Offers.AsNoTracking();

        var totalProducts = await productQuery.CountAsync(cancellationToken);
        var activeProducts = await productQuery.CountAsync(product => product.IsActive && product.Category.IsActive, cancellationToken);
        var totalCategories = await categoryQuery.CountAsync(cancellationToken);
        var activeOffers = await offerQuery.CountAsync(
            offer => offer.IsActive && offer.StartDate <= now && offer.EndDate >= now,
            cancellationToken);
        var activeProductDiscounts = await productQuery.CountAsync(
            product => product.IsActive
                && product.Category.IsActive
                && product.OfferPrice.HasValue
                && (!product.OfferStart.HasValue || product.OfferStart <= now)
                && (!product.OfferEnd.HasValue || product.OfferEnd >= now),
            cancellationToken);
        var lowStockProducts = await productQuery.CountAsync(product => product.StockQuantity > 0 && product.StockQuantity <= 5, cancellationToken);

        var recentProducts = await productQuery
            .Include(product => product.Category)
            .Include(product => product.Images)
            .OrderByDescending(product => product.CreatedAt)
            .Take(5)
            .Select(product => new AdminRecentItemDto(
                product.Id,
                product.Name,
                product.Category.Name,
                product.Images
                    .OrderBy(image => image.SortOrder)
                    .ThenByDescending(image => image.IsMain)
                    .Select(image => image.ImageUrl)
                    .FirstOrDefault(),
                product.CreatedAt,
                product.IsActive ? "Active" : "Inactive"))
            .ToListAsync(cancellationToken);

        var recentOffers = await offerQuery
            .OrderByDescending(offer => offer.CreatedAt)
            .Take(5)
            .Select(offer => new AdminRecentItemDto(
                offer.Id,
                offer.Title,
                offer.DiscountType == Entites.OfferDiscountType.Percentage
                    ? $"{offer.DiscountValue:0.##}% off"
                    : $"{offer.DiscountValue:0.##} EGP off",
                offer.ImageUrl,
                offer.CreatedAt,
                offer.IsActive ? "Active" : "Inactive"))
            .ToListAsync(cancellationToken);

        return new AdminDashboardSummaryDto(
            totalProducts,
            activeProducts,
            totalProducts - activeProducts,
            totalCategories,
            activeOffers,
            activeProductDiscounts,
            lowStockProducts,
            recentProducts,
            recentOffers);
    }

    public async Task<PagedResult<AdminProductDiscountListItemDto>> GetProductDiscountsAsync(
        AdminProductDiscountQuery query,
        CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 50);

        var discountsQuery = _context.Products
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Include(product => product.Category)
            .Include(product => product.Images)
            .Where(product => product.OfferPrice.HasValue)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            discountsQuery = discountsQuery.Where(product =>
                product.Name.Contains(search)
                || (product.Slug != null && product.Slug.Contains(search))
                || (product.Category != null && product.Category.Name.Contains(search)));
        }

        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            var status = query.Status.Trim().ToLowerInvariant();
            discountsQuery = status switch
            {
                "active" => discountsQuery.Where(product =>
                    product.IsActive
                    && product.Category.IsActive
                    && (!product.OfferStart.HasValue || product.OfferStart <= now)
                    && (!product.OfferEnd.HasValue || product.OfferEnd >= now)),
                "scheduled" => discountsQuery.Where(product =>
                    product.IsActive
                    && product.Category.IsActive
                    && product.OfferStart.HasValue
                    && product.OfferStart > now),
                "expired" => discountsQuery.Where(product =>
                    product.OfferEnd.HasValue
                    && product.OfferEnd < now),
                "inactive" => discountsQuery.Where(product =>
                    !product.IsActive
                    || !product.Category.IsActive),
                _ => discountsQuery
            };
        }

        var totalItems = await discountsQuery.CountAsync(cancellationToken);
        var items = await discountsQuery
            .OrderByDescending(product => product.OfferStart.HasValue)
            .ThenByDescending(product => product.OfferStart)
            .ThenByDescending(product => product.UpdatedAt)
            .ThenByDescending(product => product.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(product => new AdminProductDiscountListItemDto(
                product.Id,
                product.Name,
                product.Slug,
                product.CategoryId,
                product.Category != null ? product.Category.Name ?? "Uncategorized" : "Uncategorized",
                product.Images
                    .OrderBy(image => image.SortOrder)
                    .ThenByDescending(image => image.IsMain)
                    .Select(image => image.ImageUrl)
                    .FirstOrDefault(),
                product.Price,
                product.CompareAtPrice,
                product.OfferPrice ?? product.Price,
                product.OfferStart,
                product.OfferEnd,
                product.IsActive,
                product.Category != null && product.Category.IsActive,
                BuildProductDiscountStatus(product, now)))
            .ToListAsync(cancellationToken);

        var totalPages = totalItems == 0 ? 0 : (int)Math.Ceiling(totalItems / (double)pageSize);
        return new PagedResult<AdminProductDiscountListItemDto>(
            items,
            new PaginationMeta(page, pageSize, totalItems, totalPages, page > 1, totalPages > 0 && page < totalPages));
    }

    public async Task<IReadOnlyList<AdminLookupItemDto>> GetSizeOptionsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Sizes
            .AsNoTracking()
            .OrderBy(size => size.SortOrder)
            .ThenBy(size => size.Name)
            .Select(size => new AdminLookupItemDto(size.Id, size.Name, null))
            .ToListAsync(cancellationToken);
    }

    private static string BuildProductDiscountStatus(Product product, DateTimeOffset now)
    {
        if (!product.IsActive || product.Category is { IsActive: false })
        {
            return "Inactive";
        }

        if (product.OfferStart.HasValue && product.OfferStart > now)
        {
            return "Scheduled";
        }

        if (product.OfferEnd.HasValue && product.OfferEnd < now)
        {
            return "Expired";
        }

        return "Active";
    }
}
