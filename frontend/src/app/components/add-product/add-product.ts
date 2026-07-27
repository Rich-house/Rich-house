import { Component, inject, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { ProductsService } from '../../Services/product';

@Component({
  selector: 'app-add-product',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './add-product.html',
  styleUrls: ['./add-product.css']
})
export class AddProductComponent {
  private fb = inject(FormBuilder);
  private productService = inject(ProductsService);
  private router = inject(Router);
  private cdr = inject(ChangeDetectorRef);

  selectedFiles: File[] = [];

  productForm: FormGroup = this.fb.group({
    name: ['', Validators.required],
    description: ['', Validators.required],
    price: [0, [Validators.required, Validators.min(1)]],
    stockQuantity: [0, [Validators.required, Validators.min(0)]],
    categoryId: [null, Validators.required],
    selectedSizeIds: ['', Validators.required],
    mainImageIndex: [0, [Validators.required, Validators.min(0)]]
  });

  onFilesSelected(event: any) {
    if (event.target.files.length > 0) {
      this.selectedFiles = Array.from(event.target.files);
    }
  }

  onSubmit() {
    if (this.productForm.invalid || this.selectedFiles.length === 0) {
      this.productForm.markAllAsTouched();
      this.cdr.detectChanges();
      return;
    }

    const formData = new FormData();
    const formValue = this.productForm.value;

    formData.append('Name', formValue.name);
    formData.append('Description', formValue.description);
    formData.append('Price', formValue.price.toString());
    formData.append('StockQuantity', formValue.stockQuantity.toString());
    formData.append('CategoryId', formValue.categoryId.toString());
    formData.append('MainImageIndex', formValue.mainImageIndex.toString());

    const sizeIds = formValue.selectedSizeIds.split(',').map((id: string) => id.trim());
    sizeIds.forEach((id: string) => {
      if (id) {
        formData.append('SelectedSizeIds', id);
      }
    });

    this.selectedFiles.forEach((file: File) => {
      formData.append('Images', file, file.name);
    });

    this.productService.addProduct(formData).subscribe({
      next: () => {
        this.cdr.detectChanges();
        this.router.navigate(['/dashboard']);
      },
      error: (err) => {
        console.error(err);
        this.cdr.detectChanges();
      }
    });
  }

  onCancel() {
    this.router.navigate(['/dashboard']);
  }
}