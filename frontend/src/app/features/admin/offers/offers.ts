import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import Swal from 'sweetalert2';
import { AdminCatalogService } from '../../../core/services/admin-catalog';
import { describeApiError } from '../../../core/utils/http-error';
import { resolveImageUrl, productPlaceholderImage } from '../../../core/utils/image-url';
import { AdminPaginationMeta } from '../../../shared/models/admin.models';
import { AdminOfferItem, AdminOfferQuery } from '../../../shared/models/offer.models';
import { AdminProductDiscountItem, AdminProductDiscountQuery } from '../../../shared/models/product.models';
import { EgpPricePipe } from '../../../shared/pipes/egp-price.pipe';

@Component({
  selector: 'app-admin-offers',
  standalone: true,
  imports: [CommonModule, FormsModule, EgpPricePipe],
  templateUrl: './offers.html',
  styleUrl: './offers.css',
})
export class AdminOffersComponent {
  private readonly adminCatalogService = inject(AdminCatalogService);
  private readonly cdr = inject(ChangeDetectorRef);
  private readonly router = inject(Router);

  readonly fallbackImage = productPlaceholderImage;
  readonly filters = {
    search: '',
    status: '',
  };
  campaignOffers: AdminOfferItem[] = [];
  productDiscounts: AdminProductDiscountItem[] = [];
  campaignLoading = true;
  productDiscountsLoading = true;
  campaignErrorMessage = '';
  productDiscountsErrorMessage = '';
  readonly campaignQuery: Required<AdminOfferQuery> = {
    page: 1,
    pageSize: 10,
    search: '',
    status: '',
  };
  readonly productDiscountQuery: Required<AdminProductDiscountQuery> = {
    page: 1,
    pageSize: 10,
    search: '',
    status: '',
  };
  campaignPagination: AdminPaginationMeta = {
    page: 1,
    pageSize: 10,
    totalItems: 0,
    totalPages: 0,
    hasPreviousPage: false,
    hasNextPage: false,
  };
  productDiscountPagination: AdminPaginationMeta = {
    page: 1,
    pageSize: 10,
    totalItems: 0,
    totalPages: 0,
    hasPreviousPage: false,
    hasNextPage: false,
  };

  constructor() {
    this.loadCampaignOffers();
    this.loadProductDiscounts();
  }

  loadCampaignOffers(): void {
    this.campaignLoading = true;
    this.campaignErrorMessage = '';
    this.syncQueries();

    this.adminCatalogService.getOffers(this.campaignQuery).subscribe({
      next: (response) => {
        this.campaignOffers = response.items;
        this.campaignPagination = response.meta;
        this.campaignLoading = false;
        this.cdr.markForCheck();
      },
      error: (error) => {
        this.campaignLoading = false;
        this.campaignErrorMessage = describeApiError(error).message;
        this.cdr.markForCheck();
      },
    });
  }

  loadProductDiscounts(): void {
    this.productDiscountsLoading = true;
    this.productDiscountsErrorMessage = '';
    this.syncQueries();

    this.adminCatalogService.getProductDiscounts(this.productDiscountQuery).subscribe({
      next: (response) => {
        this.productDiscounts = response.items;
        this.productDiscountPagination = response.meta;
        this.productDiscountsLoading = false;
        this.cdr.markForCheck();
      },
      error: (error) => {
        this.productDiscountsLoading = false;
        this.productDiscountsErrorMessage = describeApiError(error).message;
        this.cdr.markForCheck();
      },
    });
  }

  applyFilters(): void {
    this.campaignQuery.page = 1;
    this.productDiscountQuery.page = 1;
    this.loadCampaignOffers();
    this.loadProductDiscounts();
  }

  goToCampaignPage(page: number): void {
    if (page < 1 || (this.campaignPagination.totalPages > 0 && page > this.campaignPagination.totalPages)) {
      return;
    }

    this.campaignQuery.page = page;
    this.loadCampaignOffers();
  }

  goToProductDiscountPage(page: number): void {
    if (page < 1 || (this.productDiscountPagination.totalPages > 0 && page > this.productDiscountPagination.totalPages)) {
      return;
    }

    this.productDiscountQuery.page = page;
    this.loadProductDiscounts();
  }

  addOffer(): void {
    void this.router.navigate(['/dashboard/add-offer']);
  }

  editOffer(id: number): void {
    void this.router.navigate(['/dashboard/edit-offer', id]);
  }

  toggleActivation(offer: AdminOfferItem): void {
      this.adminCatalogService.toggleOfferActivation(offer.id, !offer.isActive).subscribe({
        next: () => {
          this.loadCampaignOffers();
        },
        error: (error) => {
          void Swal.fire('Update failed', error?.error?.message || describeApiError(error).message, 'error');
        },
      });
  }

  deleteOffer(offer: AdminOfferItem): void {
    void Swal.fire({
      title: `Delete ${offer.title}?`,
      text: 'The offer record and its uploaded image will be removed.',
      icon: 'warning',
      showCancelButton: true,
      confirmButtonText: 'Delete offer',
      confirmButtonColor: '#b08a44',
    }).then((result) => {
      if (!result.isConfirmed) {
        return;
      }

      this.adminCatalogService.deleteOffer(offer.id).subscribe({
        next: () => {
          this.campaignOffers = this.campaignOffers.filter((item) => item.id !== offer.id);
          this.cdr.markForCheck();
          void Swal.fire('Deleted', 'The offer was removed successfully.', 'success');
        },
        error: (error) => {
          void Swal.fire('Delete failed', error?.error?.message || describeApiError(error).message, 'error');
        },
      });
    });
  }

  resolveImage(path?: string | null): string {
    return resolveImageUrl(path);
  }

  editProduct(id: number): void {
    void this.router.navigate(['/dashboard/edit-product', id]);
  }

  statusBadgeClass(status: string): string {
    switch (status.trim().toLowerCase()) {
      case 'active':
        return 'admin-badge admin-badge--active';
      case 'scheduled':
        return 'admin-badge admin-badge--warning';
      case 'expired':
        return 'admin-badge admin-badge--danger';
      default:
        return 'admin-badge admin-badge--muted';
    }
  }

  trackByOfferId(_index: number, offer: AdminOfferItem): number {
    return offer.id;
  }

  trackByProductDiscountId(_index: number, product: AdminProductDiscountItem): number {
    return product.id;
  }

  private syncQueries(): void {
    this.campaignQuery.search = this.filters.search;
    this.campaignQuery.status = this.filters.status;
    this.productDiscountQuery.search = this.filters.search;
    this.productDiscountQuery.status = this.filters.status;
  }
}
