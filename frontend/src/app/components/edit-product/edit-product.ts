import { Component, OnInit, inject, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { Router, ActivatedRoute } from '@angular/router';
import { ProductsService } from '../../Services/product';

@Component({
  selector: 'app-edit-product',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './edit-product.html',
  styleUrl: './edit-product.css'
})
export class EditProductComponent implements OnInit {
  private fb = inject(FormBuilder);
  private productService = inject(ProductsService);
  private router = inject(Router);
  private route = inject(ActivatedRoute);
  private cdr = inject(ChangeDetectorRef);

  productId!: number;
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

  ngOnInit(): void {
    this.productId = Number(this.route.snapshot.paramMap.get('id'));
    if (this.productId) {
      this.loadProductDetails();
    }
  }

  loadProductDetails() {
    this.productService.getProductById(this.productId).subscribe({
      next: (data) => {
        this.productForm.patchValue({
          name: data.name,
          description: data.description,
          price: data.price,
          stockQuantity: data.stockQuantity || 0,
          categoryId: data.categoryId || 0,
          selectedSizeIds: data.sizes ? data.sizes.join(', ') : '',
          mainImageIndex: data.mainImageIndex || 0
        });
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.log(err);
      }
    });
  }

  onFilesSelected(event: any) {
    if (event.target.files.length > 0) {
      this.selectedFiles = Array.from(event.target.files);
    }
  }

  onSubmit() {
    if (this.productForm.invalid) {
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

    if (this.selectedFiles.length > 0) {
      this.selectedFiles.forEach((file: File) => {
        formData.append('Images', file, file.name);
      });
    }

    this.productService.updateProduct(this.productId, formData).subscribe({
      next: () => {
        this.cdr.detectChanges();
        this.router.navigate(['/dashboard']);
      },
      error: (err) => {
        console.log(err);
        this.cdr.detectChanges();
      }
    });
  }

  onCancel() {
    this.router.navigate(['/dashboard']);
  }
}