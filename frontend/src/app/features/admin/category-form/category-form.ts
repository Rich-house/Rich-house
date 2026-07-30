import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component, DestroyRef, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import Swal from 'sweetalert2';
import { AdminCatalogService } from '../../../core/services/admin-catalog';
import { describeApiError } from '../../../core/utils/http-error';
import { resolveImageUrl } from '../../../core/utils/image-url';

@Component({
  selector: 'app-admin-category-form',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './category-form.html',
  styleUrl: './category-form.css',
})
export class AdminCategoryFormComponent {
  private readonly fb = inject(FormBuilder);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly adminCatalogService = inject(AdminCatalogService);
  private readonly cdr = inject(ChangeDetectorRef);
  private readonly destroyRef = inject(DestroyRef);

  readonly categoryId = Number(this.route.snapshot.paramMap.get('id')) || null;
  readonly isEditMode = this.categoryId !== null;

  readonly form = this.fb.group({
    name: ['', [Validators.required, Validators.minLength(2)]],
    slug: ['', [Validators.required, Validators.minLength(2)]],
    displayOrder: [0, [Validators.required, Validators.min(0)]],
    isActive: [true, Validators.required],
    removeImage: [false, Validators.required],
  });

  currentImageUrl: string | null = null;
  selectedFile: File | null = null;
  previewUrl: string | null = null;
  loading = this.isEditMode;
  saving = false;
  errorTitle = '';
  errorMessage = '';
  slugManuallyEdited = false;

  constructor() {
    this.form.controls.name.valueChanges
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((name) => {
        if (!this.slugManuallyEdited) {
          this.form.controls.slug.setValue(this.buildSlug(name || ''), { emitEvent: false });
        }
      });

    if (this.isEditMode) {
      this.loadCategory();
    } else {
      this.loading = false;
    }
  }

  get pageTitle(): string {
    return this.isEditMode ? 'Edit category' : 'Add category';
  }

  get pageSubtitle(): string {
    return this.isEditMode
      ? 'Update naming, visibility, and category imagery.'
      : 'Create a new catalog category with the correct storefront metadata.';
  }

  loadCategory(): void {
    if (!this.categoryId) {
      return;
    }

    this.loading = true;
    this.errorTitle = '';
    this.errorMessage = '';

    this.adminCatalogService.getCategoryById(this.categoryId).subscribe({
      next: (category) => {
        this.currentImageUrl = category.imageUrl;
        this.form.patchValue({
          name: category.name,
          slug: category.slug,
          displayOrder: category.displayOrder,
          isActive: category.isActive,
          removeImage: false,
        });
        this.slugManuallyEdited = true;
        this.loading = false;
        this.cdr.markForCheck();
      },
      error: (error) => {
        this.loading = false;
        if (error?.status === 404) {
          this.errorTitle = 'Category not found';
          this.errorMessage = 'This category could not be found. It may have been removed already.';
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

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0] || null;
    this.selectedFile = file;
    this.previewUrl = file ? URL.createObjectURL(file) : null;

    if (file) {
      this.form.controls.removeImage.setValue(false);
    }
  }

  onRemoveImageChange(): void {
    if (this.form.controls.removeImage.value) {
      this.previewUrl = null;
      this.selectedFile = null;
    }
  }

  onSubmit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const formData = new FormData();
    const value = this.form.getRawValue();
    formData.append('Name', (value.name || '').trim());
    formData.append('Slug', (value.slug || '').trim());
    formData.append('DisplayOrder', String(value.displayOrder));
    formData.append('IsActive', String(value.isActive));
    formData.append('RemoveImage', String(value.removeImage));

    if (this.selectedFile) {
      formData.append('Image', this.selectedFile);
    }

    this.saving = true;
    const request$ = this.isEditMode && this.categoryId
      ? this.adminCatalogService.updateCategory(this.categoryId, formData)
      : this.adminCatalogService.createCategory(formData);

    request$.subscribe({
      next: () => {
        this.saving = false;
        this.cdr.markForCheck();
        void Swal.fire('Saved', 'The category has been saved successfully.', 'success');
        void this.router.navigate(['/dashboard/categories']);
      },
      error: (error) => {
        this.saving = false;
        this.cdr.markForCheck();
        const described = describeApiError(error);
        const backendMessage =
          typeof error?.error === 'string'
            ? error.error
            : error?.error?.message || described.message;

        void Swal.fire(described.title, backendMessage, 'error');
      },
    });
  }

  resolveImage(path?: string | null): string {
    return resolveImageUrl(path);
  }

  private buildSlug(value: string): string {
    return value
      .trim()
      .toLowerCase()
      .replace(/[^a-z0-9]+/g, '-')
      .replace(/^-+|-+$/g, '');
  }
}
