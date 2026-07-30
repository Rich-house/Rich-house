import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component, DestroyRef, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { forkJoin, of } from 'rxjs';
import Swal from 'sweetalert2';
import { AdminCatalogService } from '../../../core/services/admin-catalog';
import { describeApiError } from '../../../core/utils/http-error';
import { resolveImageUrl } from '../../../core/utils/image-url';
import { AdminLookupItem } from '../../../shared/models/admin.models';
import { AdminCategoryItem } from '../../../shared/models/category.models';
import { AdminProductDetails } from '../../../shared/models/product.models';

interface EditableImageItem {
  token: string;
  url: string;
  file?: File;
  isNew: boolean;
}

interface ProductStatusOption {
  label: string;
  value: string;
}

@Component({
  selector: 'app-admin-product-form',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './product-form.html',
  styleUrl: './product-form.css',
})
export class AdminProductFormComponent {
  private readonly fb = inject(FormBuilder);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly adminCatalogService = inject(AdminCatalogService);
  private readonly cdr = inject(ChangeDetectorRef);
  private readonly destroyRef = inject(DestroyRef);

  readonly productId = Number(this.route.snapshot.paramMap.get('id')) || null;
  readonly isEditMode = this.productId !== null;
  readonly statusOptions: ProductStatusOption[] = [
    { value: 'Draft', label: 'Draft' },
    { value: 'Active', label: 'Active' },
    { value: 'Published', label: 'Published' },
    { value: 'Archived', label: 'Archived' },
  ];
  readonly form = this.fb.group({
    name: ['', [Validators.required, Validators.minLength(2)]],
    slug: ['', [Validators.required, Validators.minLength(2)]],
    shortDescription: [''],
    description: ['', [Validators.required, Validators.minLength(8)]],
    categoryId: [null as number | null, Validators.required],
    price: [0, [Validators.required, Validators.min(1)]],
    compareAtPrice: [null as number | null],
    offerPrice: [null as number | null],
    offerStart: [''],
    offerEnd: [''],
    stockQuantity: [0, [Validators.required, Validators.min(0)]],
    status: ['Draft', Validators.required],
    isActive: [true, Validators.required],
    isFeatured: [false, Validators.required],
    isBestSeller: [false, Validators.required],
    isNewArrival: [false, Validators.required],
    colors: [''],
  });

  categories: AdminCategoryItem[] = [];
  sizes: AdminLookupItem[] = [];
  selectedSizeIds = new Set<number>();
  images: EditableImageItem[] = [];
  mainImageToken: string | null = null;
  loading = true;
  saving = false;
  errorTitle = '';
  errorMessage = '';
  saveErrorMessage = '';
  slugManuallyEdited = false;
  submitAttempted = false;

  constructor() {
    this.form.controls.name.valueChanges
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((name) => {
        if (!this.slugManuallyEdited) {
          this.form.controls.slug.setValue(this.buildSlug(name || ''), { emitEvent: false });
        }
      });

    this.loadFormData();
  }

  get pageTitle(): string {
    return this.isEditMode ? 'Edit product' : 'Add product';
  }

  get pageSubtitle(): string {
    return this.isEditMode
      ? 'Update pricing, imagery, stock, and merchandising flags.'
      : 'Create a complete product record with sizes, colors, and gallery media.';
  }

  get submitBlockedReasons(): string[] {
    const reasons: string[] = [];

    if (this.form.controls.name.invalid) {
      reasons.push('Enter a product name with at least 2 characters.');
    }

    if (this.form.controls.slug.invalid) {
      reasons.push('Provide a valid product slug.');
    }

    if (this.form.controls.categoryId.invalid) {
      reasons.push('Select a category before saving.');
    }

    if (this.form.controls.description.invalid) {
      reasons.push('Add a fuller product description before saving.');
    }

    if (this.form.controls.price.invalid) {
      reasons.push('Base price must be greater than zero.');
    }

    if (this.form.controls.stockQuantity.invalid) {
      reasons.push('Stock quantity cannot be negative.');
    }

    if (this.images.length === 0) {
      reasons.push('Keep at least one product image.');
    }

    return reasons;
  }

  get showSubmitValidation(): boolean {
    return !this.saving && this.submitAttempted && this.submitBlockedReasons.length > 0;
  }

  loadFormData(): void {
    this.loading = true;
    this.errorTitle = '';
    this.errorMessage = '';
    this.saveErrorMessage = '';
    this.submitAttempted = false;

    const lookups$ = forkJoin({
      categories: this.adminCatalogService.getCategories(),
      sizes: this.adminCatalogService.getSizeOptions(),
    });

    const request$ = this.isEditMode && this.productId
      ? forkJoin({
          lookups: lookups$,
          product: this.adminCatalogService.getProductById(this.productId),
        })
      : forkJoin({
          lookups: lookups$,
          product: of(null as AdminProductDetails | null),
        });

    request$.subscribe({
      next: (result) => {
        const lookups = result.lookups;
        this.categories = lookups.categories;
        this.sizes = lookups.sizes;

        const product = result.product;
        if (product) {
          this.populateProduct(product);
        }

        this.loading = false;
        this.cdr.markForCheck();
      },
      error: (error) => {
        this.loading = false;
        if (error?.status === 404) {
          this.errorTitle = 'Product not found';
          this.errorMessage = 'This product could not be found. It may have been removed already.';
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

  onSlugInput(): void {
    this.slugManuallyEdited = true;
  }

  onToggleSize(sizeId: number, checked: boolean): void {
    if (checked) {
      this.selectedSizeIds.add(sizeId);
      return;
    }

    this.selectedSizeIds.delete(sizeId);
  }

  hasSize(sizeId: number): boolean {
    return this.selectedSizeIds.has(sizeId);
  }

  onFilesSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const files = Array.from(input.files || []);

    files.forEach((file, index) => {
      this.images.push({
        token: `pending-${Date.now()}-${index}`,
        url: URL.createObjectURL(file),
        file,
        isNew: true,
      });
    });

    if (!this.mainImageToken && this.images.length > 0) {
      this.mainImageToken = this.images[0].token;
    }
  }

  setMainImage(token: string): void {
    this.mainImageToken = token;
  }

  moveImage(index: number, direction: -1 | 1): void {
    const targetIndex = index + direction;
    if (targetIndex < 0 || targetIndex >= this.images.length) {
      return;
    }

    const [image] = this.images.splice(index, 1);
    this.images.splice(targetIndex, 0, image);
  }

  removeImage(token: string): void {
    this.images = this.images.filter((image) => image.token !== token);
    if (this.mainImageToken === token) {
      this.mainImageToken = this.images[0]?.token ?? null;
    }
  }

  onSubmit(): void {
    this.submitAttempted = true;
    this.saveErrorMessage = '';

    if (this.form.invalid || this.images.length === 0) {
      this.form.markAllAsTouched();
      this.cdr.markForCheck();
      return;
    }

    const formData = this.buildFormData();
    this.saving = true;

    const request$ = this.isEditMode && this.productId
      ? this.adminCatalogService.updateProduct(this.productId, formData)
      : this.adminCatalogService.createProduct(formData);

    request$.subscribe({
      next: () => {
        this.saving = false;
        this.cdr.markForCheck();
        void Swal.fire('Saved', 'The product has been saved successfully.', 'success');
        void this.router.navigate(['/dashboard/products']);
      },
      error: (error) => {
        this.saving = false;
        this.cdr.markForCheck();
        const described = describeApiError(error);
        this.saveErrorMessage =
          typeof error?.error === 'string'
            ? error.error
            : error?.error?.message || described.message;

        void Swal.fire(described.title, this.saveErrorMessage, 'error');
      },
    });
  }

  resolveImage(path: string): string {
    return resolveImageUrl(path);
  }

  trackByCategoryId(_index: number, category: AdminCategoryItem): number {
    return category.id;
  }

  trackBySizeId(_index: number, size: AdminLookupItem): number {
    return size.id;
  }

  trackByImageToken(_index: number, image: EditableImageItem): string {
    return image.token;
  }

  private populateProduct(product: AdminProductDetails): void {
    this.form.patchValue({
      name: product.name,
      slug: product.slug,
      shortDescription: product.shortDescription,
      description: product.description,
      categoryId: product.categoryId,
      price: product.price,
      compareAtPrice: product.compareAtPrice,
      offerPrice: product.offerPrice,
      offerStart: this.toDateTimeLocal(product.offerStart),
      offerEnd: this.toDateTimeLocal(product.offerEnd),
      stockQuantity: product.stockQuantity,
      status: product.status,
      isActive: product.isActive,
      isFeatured: product.isFeatured,
      isBestSeller: product.isBestSeller,
      isNewArrival: product.isNewArrival,
      colors: product.colors.join(', '),
    });
    this.slugManuallyEdited = true;
    this.selectedSizeIds = new Set(product.selectedSizeIds);
    this.images = product.images.map((image) => ({
      token: `existing:${image.url}`,
      url: image.url,
      isNew: false,
    }));
    this.mainImageToken = this.images.find((image) =>
      product.images.some((entry) => entry.url === image.url && entry.isMain),
    )?.token || this.images[0]?.token || null;
  }

  private buildFormData(): FormData {
    const rawValue = this.form.getRawValue();
    const formData = new FormData();

    formData.append('Name', (rawValue.name || '').trim());
    formData.append('Slug', (rawValue.slug || '').trim());
    formData.append('ShortDescription', rawValue.shortDescription?.trim() || '');
    formData.append('Description', (rawValue.description || '').trim());
    formData.append('CategoryId', String(rawValue.categoryId));
    formData.append('Price', String(rawValue.price));
    formData.append('StockQuantity', String(rawValue.stockQuantity));
    formData.append('Status', rawValue.status?.trim() || 'Draft');
    formData.append('IsActive', String(rawValue.isActive));
    formData.append('IsFeatured', String(rawValue.isFeatured));
    formData.append('IsBestSeller', String(rawValue.isBestSeller));
    formData.append('IsNewArrival', String(rawValue.isNewArrival));

    if (rawValue.compareAtPrice !== null && rawValue.compareAtPrice !== undefined) {
      formData.append('CompareAtPrice', String(rawValue.compareAtPrice));
    }

    if (rawValue.offerPrice !== null && rawValue.offerPrice !== undefined) {
      formData.append('OfferPrice', String(rawValue.offerPrice));
    }

    if (rawValue.offerStart) {
      formData.append('OfferStart', new Date(rawValue.offerStart).toISOString());
    }

    if (rawValue.offerEnd) {
      formData.append('OfferEnd', new Date(rawValue.offerEnd).toISOString());
    }

    Array.from(this.selectedSizeIds).forEach((sizeId) => {
      formData.append('SelectedSizeIds', String(sizeId));
    });

    this.parseColors(rawValue.colors || '').forEach((color) => {
      formData.append('Colors', color);
    });

    let newImageIndex = 0;
    let mappedMainToken: string | null = null;
    this.images.forEach((image) => {
      if (image.isNew && image.file) {
        const token = `new:${newImageIndex++}`;
        if (this.mainImageToken === image.token) {
          mappedMainToken = token;
        }

        formData.append('Images', image.file);
        formData.append('ImageOrder', token);
        return;
      }

      const token = `existing:${image.url}`;
      if (this.mainImageToken === image.token) {
        mappedMainToken = token;
      }

      formData.append('ImageOrder', token);
    });

    formData.append('MainImageReference', mappedMainToken || 'existing:');
    return formData;
  }

  private parseColors(value: string): string[] {
    return value
      .split(',')
      .map((item) => item.trim())
      .filter((item, index, items) => !!item && items.indexOf(item) === index);
  }

  private buildSlug(value: string): string {
    return value
      .trim()
      .toLowerCase()
      .replace(/[^a-z0-9]+/g, '-')
      .replace(/^-+|-+$/g, '');
  }

  private toDateTimeLocal(value: string | null): string {
    if (!value) {
      return '';
    }

    const date = new Date(value);
    if (Number.isNaN(date.getTime())) {
      return '';
    }

    const pad = (input: number) => input.toString().padStart(2, '0');
    return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}T${pad(date.getHours())}:${pad(date.getMinutes())}`;
  }
}
