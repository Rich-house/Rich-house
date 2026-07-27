import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { ProductsService } from '../../../core/services/product';
import Swal from 'sweetalert2';

@Component({
  selector: 'app-add-category',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './add-category.html',
  styleUrl: './add-category.css'
})
export class AddCategoryComponent {
  categoryName: string = '';
  private service = inject(ProductsService);
  private router = inject(Router);

  onSave() {
    this.service.addCategory({ name: this.categoryName }).subscribe({
      next: () => {
        Swal.fire('Success', 'Category added successfully!', 'success');
        this.router.navigate(['/dashboard']);
      },
      error: () => Swal.fire('Error', 'Failed to add category', 'error')
    });
  }

  onCancel() {
    this.router.navigate(['/dashboard']);
  }
}