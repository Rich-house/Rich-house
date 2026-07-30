import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import Swal from 'sweetalert2';
import { AdminCatalogService } from '../../../core/services/admin-catalog';
import { describeApiError } from '../../../core/utils/http-error';
import { resolveImageUrl, productPlaceholderImage } from '../../../core/utils/image-url';
import { AdminCategoryItem } from '../../../shared/models/category.models';
import { AdminProductListItem, AdminProductQuery } from '../../../shared/models/product.models';
import { EgpPricePipe } from '../../../shared/pipes/egp-price.pipe';

@Component({
  selector: 'app-admin-products',
  standalone: true,
  imports: [CommonModule, FormsModule, EgpPricePipe],
  templateUrl: './products.html',
  styleUrl: './products.css',
})
export class AdminProductsComponent {
  private readonly adminCatalogService = inject(AdminCatalogService);
  private readonly cdr = inject(ChangeDetectorRef);
  private readonly router = inject(Router);

  categories: AdminCategoryItem[] = [];
  products: AdminProductListItem[] = [];
  loading = true;
  errorMessage = '';
  readonly fallbackImage = productPlaceholderImage;
  readonly query: Required<AdminProductQuery> = {
    page: 1,
    pageSize: 10,
    search: '',
    categoryId: null,
    isActive: null,
    lowStockOnly: false,
  };
  pagination = {
    page: 1,
    pageSize: 10,
    totalItems: 0,
    totalPages: 0,
    hasPreviousPage: false,
    hasNextPage: false,
  };

  constructor() {
    this.loadCategories();
    this.loadProducts();
  }

  loadCategories(): void {
    this.adminCatalogService.getCategories().subscribe({
      next: (categories) => {
        this.categories = categories;
        this.cdr.markForCheck();
      },
      error: () => {
        this.categories = [];
        this.cdr.markForCheck();
      },
    });
  }

  loadProducts(): void {
    this.loading = true;
    this.errorMessage = '';

    this.adminCatalogService.getProducts(this.query).subscribe({
      next: (response) => {
        this.products = response.items;
        this.pagination = response.meta;
        this.loading = false;
        this.cdr.markForCheck();
      },
      error: (error) => {
        this.loading = false;
        this.errorMessage = describeApiError(error).message;
        this.cdr.markForCheck();
      },
    });
  }

  applyFilters(): void {
    this.query.page = 1;
    this.loadProducts();
  }

  goToPage(page: number): void {
    if (page < 1 || (this.pagination.totalPages > 0 && page > this.pagination.totalPages)) {
      return;
    }

    this.query.page = page;
    this.loadProducts();
  }

  addProduct(): void {
    void this.router.navigate(['/dashboard/add-product']);
  }

  editProduct(id: number): void {
    void this.router.navigate(['/dashboard/edit-product', id]);
  }

  deleteProduct(product: AdminProductListItem): void {
    void Swal.fire({
      title: `Delete ${product.name}?`,
      text: 'The product record and its uploaded images will be removed.',
      icon: 'warning',
      showCancelButton: true,
      confirmButtonText: 'Delete product',
      confirmButtonColor: '#b08a44',
    }).then((result) => {
      if (!result.isConfirmed) {
        return;
      }

      this.adminCatalogService.deleteProduct(product.id).subscribe({
        next: () => {
          this.products = this.products.filter((item) => item.id !== product.id);
          this.cdr.markForCheck();
          void Swal.fire('Deleted', 'The product was removed successfully.', 'success');
          if (this.products.length === 0 && this.query.page > 1) {
            this.goToPage(this.query.page - 1);
          }
        },
        error: (error) => {
          const message = error?.error?.message || describeApiError(error).message;
          void Swal.fire('Delete failed', message, 'error');
        },
      });
    });
  }

  resolveImage(path?: string | null): string {
    return resolveImageUrl(path);
  }

  trackByProductId(_index: number, product: AdminProductListItem): number {
    return product.id;
  }
}
