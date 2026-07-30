import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { forkJoin, of } from 'rxjs';
import Swal from 'sweetalert2';
import { AdminCatalogService } from '../../../core/services/admin-catalog';
import { describeApiError } from '../../../core/utils/http-error';
import { resolveImageUrl } from '../../../core/utils/image-url';
import { AdminCategoryItem } from '../../../shared/models/category.models';
import { AdminOfferDetails } from '../../../shared/models/offer.models';
import { AdminProductListItem } from '../../../shared/models/product.models';

@Component({
  selector: 'app-admin-offer-form',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './offer-form.html',
  styleUrl: './offer-form.css',
})
export class AdminOfferFormComponent {
  private readonly fb = inject(FormBuilder);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly adminCatalogService = inject(AdminCatalogService);
  private readonly cdr = inject(ChangeDetectorRef);

  readonly offerId = Number(this.route.snapshot.paramMap.get('id')) || null;
  readonly isEditMode = this.offerId !== null;
  readonly form = this.fb.group({
    title: ['', [Validators.required, Validators.minLength(2)]],
    description: [''],
    discountType: ['Percentage', Validators.required],
    discountValue: [0, [Validators.required, Validators.min(0.01)]],
    startDate: ['', Validators.required],
    endDate: ['', Validators.required],
    isActive: [true, Validators.required],
    removeImage: [false, Validators.required],
    productSearch: [''],
    categorySearch: [''],
  });

  categories: AdminCategoryItem[] = [];
  products: AdminProductListItem[] = [];
  selectedProductIds = new Set<number>();
  selectedCategoryIds = new Set<number>();
  currentImageUrl: string | null = null;
  previewUrl: string | null = null;
  selectedFile: File | null = null;
  loading = true;
  saving = false;
  errorTitle = '';
  errorMessage = '';

  constructor() {
    this.loadFormData();
  }

  get filteredProducts(): AdminProductListItem[] {
    const search = (this.form.controls.productSearch.value || '').trim().toLowerCase();
    if (!search) {
      return this.products;
    }

    return this.products.filter((product) =>
      product.name.toLowerCase().includes(search) || product.categoryName.toLowerCase().includes(search),
    );
  }

  get filteredCategories(): AdminCategoryItem[] {
    const search = (this.form.controls.categorySearch.value || '').trim().toLowerCase();
    if (!search) {
      return this.categories;
    }

    return this.categories.filter((category) => category.name.toLowerCase().includes(search));
  }

  get pageTitle(): string {
    return this.isEditMode ? 'Edit offer' : 'Add offer';
  }

  loadFormData(): void {
    this.loading = true;
    this.errorTitle = '';
    this.errorMessage = '';

    const lookups$ = forkJoin({
      categories: this.adminCatalogService.getCategories(),
      products: this.adminCatalogService.getProducts({ page: 1, pageSize: 200 }),
    });

    const request$ = this.isEditMode && this.offerId
      ? forkJoin({
          lookups: lookups$,
          offer: this.adminCatalogService.getOfferById(this.offerId),
        })
      : forkJoin({
          lookups: lookups$,
          offer: of(null as AdminOfferDetails | null),
        });

    request$.subscribe({
      next: (result) => {
        this.categories = result.lookups.categories;
        this.products = result.lookups.products.items;

        if (result.offer) {
          this.populateOffer(result.offer);
        }

        this.loading = false;
        this.cdr.markForCheck();
      },
      error: (error) => {
        this.loading = false;
        if (error?.status === 404) {
          this.errorTitle = 'Offer not found';
          this.errorMessage = 'This offer could not be found. It may have been removed already.';
          this.cdr.markForCheck();
          return;
        }

        const described = describeApiError(error);
        this.errorTitle = described.title;
        this.errorMessage = described.message;
        this.cdr.markForCheck();
      },
    });
  }

  onToggleProduct(productId: number, checked: boolean): void {
    if (checked) {
      this.selectedProductIds.add(productId);
      return;
    }

    this.selectedProductIds.delete(productId);
  }

  onToggleCategory(categoryId: number, checked: boolean): void {
    if (checked) {
      this.selectedCategoryIds.add(categoryId);
      return;
    }

    this.selectedCategoryIds.delete(categoryId);
  }

  hasProduct(productId: number): boolean {
    return this.selectedProductIds.has(productId);
  }

  hasCategory(categoryId: number): boolean {
    return this.selectedCategoryIds.has(categoryId);
  }

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0] || null;
    this.selectedFile = file;
    this.previewUrl = file ? URL.createObjectURL(file) : null;

    if (file) {
      this.form.controls.removeImage.setValue(false);
    }
  }

  onSubmit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const startDate = new Date(this.form.controls.startDate.value || '');
    const endDate = new Date(this.form.controls.endDate.value || '');
    if (Number.isNaN(startDate.getTime()) || Number.isNaN(endDate.getTime()) || endDate <= startDate) {
      void Swal.fire('Invalid dates', 'Offer end date must be after the start date.', 'error');
      return;
    }

    const formData = new FormData();
    const rawValue = this.form.getRawValue();
    formData.append('Title', (rawValue.title || '').trim());
    formData.append('Description', rawValue.description?.trim() || '');
    formData.append('DiscountType', rawValue.discountType || 'Percentage');
    formData.append('DiscountValue', String(rawValue.discountValue));
    formData.append('StartDate', startDate.toISOString());
    formData.append('EndDate', endDate.toISOString());
    formData.append('IsActive', String(rawValue.isActive));
    formData.append('RemoveImage', String(rawValue.removeImage));

    Array.from(this.selectedProductIds).forEach((productId) => formData.append('ProductIds', String(productId)));
    Array.from(this.selectedCategoryIds).forEach((categoryId) => formData.append('CategoryIds', String(categoryId)));

    if (this.selectedFile) {
      formData.append('Image', this.selectedFile);
    }

    this.saving = true;
    const request$ = this.isEditMode && this.offerId
      ? this.adminCatalogService.updateOffer(this.offerId, formData)
      : this.adminCatalogService.createOffer(formData);

    request$.subscribe({
      next: () => {
        this.saving = false;
        this.cdr.markForCheck();
        void Swal.fire('Saved', 'The offer has been saved successfully.', 'success');
        void this.router.navigate(['/dashboard/offers']);
      },
      error: (error) => {
        this.saving = false;
        this.cdr.markForCheck();
        const described = describeApiError(error);
        const message =
          typeof error?.error === 'string'
            ? error.error
            : error?.error?.message || described.message;

        void Swal.fire(described.title, message, 'error');
      },
    });
  }

  resolveImage(path?: string | null): string {
    return resolveImageUrl(path);
  }

  trackByCategoryId(_index: number, category: AdminCategoryItem): number {
    return category.id;
  }

  trackByProductId(_index: number, product: AdminProductListItem): number {
    return product.id;
  }

  private populateOffer(offer: AdminOfferDetails): void {
    this.currentImageUrl = offer.imageUrl;
    this.selectedProductIds = new Set(offer.productIds);
    this.selectedCategoryIds = new Set(offer.categoryIds);
    this.form.patchValue({
      title: offer.title,
      description: offer.description,
      discountType: offer.discountType,
      discountValue: offer.discountValue,
      startDate: this.toDateTimeLocal(offer.startDate),
      endDate: this.toDateTimeLocal(offer.endDate),
      isActive: offer.isActive,
      removeImage: false,
    });
  }

  private toDateTimeLocal(value: string): string {
    const date = new Date(value);
    if (Number.isNaN(date.getTime())) {
      return '';
    }

    const pad = (input: number) => input.toString().padStart(2, '0');
    return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}T${pad(date.getHours())}:${pad(date.getMinutes())}`;
  }
}
