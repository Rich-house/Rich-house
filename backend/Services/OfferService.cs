using Marketify.Contracts.Common;
using Marketify.Contracts.Offer;
using Marketify.Date;
using Marketify.Entites;
using Microsoft.EntityFrameworkCore;

namespace Marketify.Services;

public sealed class OfferService(
    ApplicationDbContext context,
    CatalogImageStorageService imageStorage) : IOfferService
{
    private readonly ApplicationDbContext _context = context;
    private readonly CatalogImageStorageService _imageStorage = imageStorage;

    public async Task<PagedResult<AdminOfferListItemDto>> GetAdminOffersAsync(AdminOfferQuery query, CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 50);

        var offersQuery = _context.Offers
            .AsNoTracking()
            .Include(offer => offer.Products)
            .Include(offer => offer.Categories)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            offersQuery = offersQuery.Where(offer =>
                offer.Title.Contains(search) ||
                (offer.Description != null && offer.Description.Contains(search)));
        }

        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            var status = query.Status.Trim().ToLowerInvariant();
            offersQuery = status switch
            {
                "active" => offersQuery.Where(offer => offer.IsActive && offer.StartDate <= now && offer.EndDate >= now),
                "scheduled" => offersQuery.Where(offer => offer.IsActive && offer.StartDate > now),
                "expired" => offersQuery.Where(offer => offer.EndDate < now),
                "inactive" => offersQuery.Where(offer => !offer.IsActive),
                _ => offersQuery
            };
        }

        var totalItems = await offersQuery.CountAsync(cancellationToken);
        var items = await offersQuery
            .OrderByDescending(offer => offer.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(offer => new AdminOfferListItemDto(
                offer.Id,
                offer.Title,
                offer.ImageUrl,
                offer.DiscountType.ToString(),
                offer.DiscountValue,
                offer.StartDate,
                offer.EndDate,
                offer.IsActive,
                offer.Products.Count,
                offer.Categories.Count,
                BuildStatus(offer, now)))
            .ToListAsync(cancellationToken);

        var totalPages = totalItems == 0 ? 0 : (int)Math.Ceiling(totalItems / (double)pageSize);
        return new PagedResult<AdminOfferListItemDto>(
            items,
            new PaginationMeta(page, pageSize, totalItems, totalPages, page > 1, totalPages > 0 && page < totalPages));
    }

    public async Task<AdminOfferDetailsDto?> GetAdminOfferByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Offers
            .AsNoTracking()
            .Include(offer => offer.Products)
            .Include(offer => offer.Categories)
            .Where(offer => offer.Id == id)
            .Select(offer => new AdminOfferDetailsDto(
                offer.Id,
                offer.Title,
                offer.Description,
                offer.ImageUrl,
                offer.DiscountType.ToString(),
                offer.DiscountValue,
                offer.StartDate,
                offer.EndDate,
                offer.IsActive,
                offer.Products.Select(link => link.ProductId).ToList(),
                offer.Categories.Select(link => link.CategoryId).ToList()))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<AdminOfferDetailsDto> CreateOfferAsync(UpsertOfferDto dto, CancellationToken cancellationToken = default)
    {
        Validate(dto);
        await EnsureReferencedEntitiesExistAsync(dto, cancellationToken);

        var offer = new Offer
        {
            Title = dto.Title.Trim(),
            Description = NormalizeOptionalText(dto.Description),
            DiscountType = ParseDiscountType(dto.DiscountType),
            DiscountValue = dto.DiscountValue,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            IsActive = dto.IsActive
        };

        StoredImageFile? newImage = null;

        try
        {
            if (dto.Image is not null)
            {
                newImage = await _imageStorage.SaveImageAsync(dto.Image, "offers", offer.Title, cancellationToken);
                offer.ImageUrl = newImage.Value.RelativePath;
            }

            ApplyLinks(offer, dto);
            _context.Offers.Add(offer);
            await _context.SaveChangesAsync(cancellationToken);

            return await BuildDetailsAsync(offer.Id, cancellationToken);
        }
        catch
        {
            if (newImage is not null)
            {
                _imageStorage.DeleteIfExists(newImage.Value.RelativePath);
            }

            throw;
        }
    }

    public async Task<AdminOfferDetailsDto> UpdateOfferAsync(int id, UpsertOfferDto dto, CancellationToken cancellationToken = default)
    {
        Validate(dto);
        await EnsureReferencedEntitiesExistAsync(dto, cancellationToken);

        var offer = await _context.Offers
            .Include(existing => existing.Products)
            .Include(existing => existing.Categories)
            .SingleOrDefaultAsync(existing => existing.Id == id, cancellationToken);
        if (offer is null)
        {
            throw new InvalidOperationException("Offer not found.");
        }

        offer.Title = dto.Title.Trim();
        offer.Description = NormalizeOptionalText(dto.Description);
        offer.DiscountType = ParseDiscountType(dto.DiscountType);
        offer.DiscountValue = dto.DiscountValue;
        offer.StartDate = dto.StartDate;
        offer.EndDate = dto.EndDate;
        offer.IsActive = dto.IsActive;
        offer.UpdatedAt = DateTimeOffset.UtcNow;

        var previousImageUrl = offer.ImageUrl;
        StoredImageFile? newImage = null;

        try
        {
            if (dto.RemoveImage)
            {
                offer.ImageUrl = null;
            }

            if (dto.Image is not null)
            {
                newImage = await _imageStorage.SaveImageAsync(dto.Image, "offers", offer.Title, cancellationToken);
                offer.ImageUrl = newImage.Value.RelativePath;
            }

            ApplyLinks(offer, dto);
            await _context.SaveChangesAsync(cancellationToken);

            if (!string.Equals(previousImageUrl, offer.ImageUrl, StringComparison.OrdinalIgnoreCase)
                && !string.IsNullOrWhiteSpace(previousImageUrl))
            {
                _imageStorage.DeleteIfExists(previousImageUrl);
            }

            return await BuildDetailsAsync(offer.Id, cancellationToken);
        }
        catch
        {
            if (newImage is not null)
            {
                _imageStorage.DeleteIfExists(newImage.Value.RelativePath);
            }

            offer.ImageUrl = previousImageUrl;
            throw;
        }
    }

    public async Task DeleteOfferAsync(int id, CancellationToken cancellationToken = default)
    {
        var offer = await _context.Offers.SingleOrDefaultAsync(existing => existing.Id == id, cancellationToken);
        if (offer is null)
        {
            throw new InvalidOperationException("Offer not found.");
        }

        var imageUrl = offer.ImageUrl;
        _context.Offers.Remove(offer);
        await _context.SaveChangesAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(imageUrl))
        {
            _imageStorage.DeleteIfExists(imageUrl);
        }
    }

    public async Task ToggleOfferAsync(int id, bool isActive, CancellationToken cancellationToken = default)
    {
        var offer = await _context.Offers.SingleOrDefaultAsync(existing => existing.Id == id, cancellationToken);
        if (offer is null)
        {
            throw new InvalidOperationException("Offer not found.");
        }

        offer.IsActive = isActive;
        offer.UpdatedAt = DateTimeOffset.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task<AdminOfferDetailsDto> BuildDetailsAsync(int offerId, CancellationToken cancellationToken)
    {
        return await GetAdminOfferByIdAsync(offerId, cancellationToken)
            ?? throw new InvalidOperationException("Offer could not be loaded after saving.");
    }

    private static void ApplyLinks(Offer offer, UpsertOfferDto dto)
    {
        offer.Products.Clear();
        foreach (var productId in dto.ProductIds.Distinct())
        {
            offer.Products.Add(new OfferProduct { OfferId = offer.Id, ProductId = productId });
        }

        offer.Categories.Clear();
        foreach (var categoryId in dto.CategoryIds.Distinct())
        {
            offer.Categories.Add(new OfferCategory { OfferId = offer.Id, CategoryId = categoryId });
        }
    }

    private async Task EnsureReferencedEntitiesExistAsync(UpsertOfferDto dto, CancellationToken cancellationToken)
    {
        var distinctProductIds = dto.ProductIds.Distinct().ToArray();
        var distinctCategoryIds = dto.CategoryIds.Distinct().ToArray();

        if (distinctProductIds.Length > 0)
        {
            var productCount = await _context.Products
                .IgnoreQueryFilters()
                .CountAsync(product => distinctProductIds.Contains(product.Id), cancellationToken);
            if (productCount != distinctProductIds.Length)
            {
                throw new InvalidOperationException("One or more selected products no longer exist.");
            }
        }

        if (distinctCategoryIds.Length > 0)
        {
            var categoryCount = await _context.Categories
                .CountAsync(category => distinctCategoryIds.Contains(category.Id), cancellationToken);
            if (categoryCount != distinctCategoryIds.Length)
            {
                throw new InvalidOperationException("One or more selected categories no longer exist.");
            }
        }
    }

    private static void Validate(UpsertOfferDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Title))
        {
            throw new InvalidOperationException("Offer title is required.");
        }

        if (dto.EndDate <= dto.StartDate)
        {
            throw new InvalidOperationException("Offer end date must be after the start date.");
        }

        if (dto.DiscountValue <= 0)
        {
            throw new InvalidOperationException("Discount value must be greater than zero.");
        }

        var discountType = ParseDiscountType(dto.DiscountType);
        if (discountType == OfferDiscountType.Percentage && dto.DiscountValue > 100)
        {
            throw new InvalidOperationException("Percentage discounts cannot exceed 100.");
        }
    }

    private static OfferDiscountType ParseDiscountType(string? value)
    {
        return value?.Trim().ToLowerInvariant() switch
        {
            "fixedamount" or "fixed" or "amount" => OfferDiscountType.FixedAmount,
            _ => OfferDiscountType.Percentage
        };
    }

    private static string BuildStatus(Offer offer, DateTimeOffset now)
    {
        if (!offer.IsActive)
        {
            return "Inactive";
        }

        if (offer.EndDate < now)
        {
            return "Expired";
        }

        if (offer.StartDate > now)
        {
            return "Scheduled";
        }

        return "Active";
    }

    private static string? NormalizeOptionalText(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
