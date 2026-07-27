import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { ProductsService } from '../../Services/product';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import Swal from 'sweetalert2';

@Component({
  selector: 'app-edit-category',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule],
  templateUrl: './edit-category.html',
  styleUrl: './edit-category.css',
})
export class EditCategory implements OnInit {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private productService = inject(ProductsService);
  private fb = inject(FormBuilder);

  categoryForm: FormGroup;
  categoryId!: number;
  isLoading = true;

  constructor() {
    // بناء الفورم
    this.categoryForm = this.fb.group({
      name: ['', [Validators.required, Validators.minLength(3)]]
    });
  }

  ngOnInit(): void {
    // الحصول على الـ ID من الرابط
    this.categoryId = Number(this.route.snapshot.paramMap.get('id'));
    
    if (this.categoryId) {
      this.loadCategoryData();
    }
  }

 loadCategoryData() {
  this.productService.getCategoryById(this.categoryId).subscribe({
    next: (data) => {
      console.log('Category Data:', data); 
      this.categoryForm.patchValue({
        name: data.name 
      });
      this.isLoading = false;
    },
    error: (err) => {
      console.error(err);
      Swal.fire('Error', 'Could not load category data', 'error');
      this.router.navigate(['/dashboard']);
    }
  });
}

  onSubmit() {
    if (this.categoryForm.valid) {
      this.productService.updateCategory(this.categoryId, this.categoryForm.value).subscribe({
        next: () => {
          Swal.fire({
            icon: 'success',
            title: 'Updated!',
            text: 'Category updated successfully',
            timer: 1500,
            showConfirmButton: false
          });
          this.router.navigate(['/dashboard']);
        },
        error: () => Swal.fire('Error', 'Update failed', 'error')
      });
    }
  }

  goBack() {
    this.router.navigate(['/dashboard']);
  }
}