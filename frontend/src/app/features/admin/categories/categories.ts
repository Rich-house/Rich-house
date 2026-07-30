import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component, inject } from '@angular/core';
import { Router } from '@angular/router';
import Swal from 'sweetalert2';
import { AdminCatalogService } from '../../../core/services/admin-catalog';
import { AdminCategoryItem } from '../../../shared/models/category.models';
import { resolveImageUrl, productPlaceholderImage } from '../../../core/utils/image-url';
import { describeApiError } from '../../../core/utils/http-error';

@Component({
  selector: 'app-admin-categories',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './categories.html',
  styleUrl: './categories.css',
})
export class AdminCategoriesComponent {
  private readonly adminCatalogService = inject(AdminCatalogService);
  private readonly cdr = inject(ChangeDetectorRef);
  private readonly router = inject(Router);

  categories: AdminCategoryItem[] = [];
  loading = true;
  errorMessage = '';
  readonly fallbackImage = productPlaceholderImage;

  constructor() {
    this.loadCategories();
  }

  loadCategories(): void {
    this.loading = true;
    this.errorMessage = '';

    this.adminCatalogService.getCategories().subscribe({
      next: (categories) => {
        this.categories = categories;
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

  addCategory(): void {
    void this.router.navigate(['/dashboard/add-category']);
  }

  editCategory(id: number): void {
    void this.router.navigate(['/dashboard/edit-category', id]);
  }

  deleteCategory(category: AdminCategoryItem): void {
    void Swal.fire({
      title: `Delete ${category.name}?`,
      text: 'This category will be removed only if it is not linked to products or offers.',
      icon: 'warning',
      showCancelButton: true,
      confirmButtonText: 'Delete category',
      confirmButtonColor: '#b08a44',
    }).then((result) => {
      if (!result.isConfirmed) {
        return;
      }

      this.adminCatalogService.deleteCategory(category.id).subscribe({
        next: () => {
          this.categories = this.categories.filter((item) => item.id !== category.id);
          this.cdr.markForCheck();
          void Swal.fire('Deleted', 'The category was removed successfully.', 'success');
        },
        error: (error) => {
          const backendMessage = error?.error?.message || describeApiError(error).message;
          void Swal.fire('Unable to delete category', backendMessage, 'error');
        },
      });
    });
  }

  resolveImage(path?: string | null): string {
    return resolveImageUrl(path);
  }

  trackByCategoryId(_index: number, category: AdminCategoryItem): number {
    return category.id;
  }
}
