import { Component, OnInit, inject, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { ProductsService } from '../../../core/services/product';
import { Auth } from '../../../core/services/auth'; 
import Swal from 'sweetalert2';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.css',
})
export class Dashboard implements OnInit {
  private productservices = inject(ProductsService);
  private authService = inject(Auth);
  private cdr = inject(ChangeDetectorRef);
  private router = inject(Router);

  activeTab: 'products' | 'users' | 'categories' = 'products';

  products: any[] = [];
  users: any[] = [];
  categories: any[] = [];
  searchEmail: string = '';
  searchResult: any = null;
  readonly canManageUsers = this.authService.isSuperAdmin();

  ngOnInit(): void {
    this.loadproducts();
    if (this.canManageUsers) {
      this.loadAllUsers();
    }
    this.loadCategories();
  }

  // --- Category Logic ---
  loadCategories() {
    this.productservices.getCategories().subscribe({
      next: (data) => {
        this.categories = data;
        this.cdr.detectChanges();
      },
      error: () => undefined,
    });
  }

  deleteCategory(id: number) {
    Swal.fire({
      title: 'Are you sure?',
      text: "This will remove the category!",
      icon: 'warning',
      showCancelButton: true,
      confirmButtonColor: '#d33',
      confirmButtonText: 'Yes, delete it!'
    }).then((result) => {
      if (result.isConfirmed) {
        this.productservices.deleteCategory(id).subscribe({
          next: () => {
            this.categories = this.categories.filter(c => c.id !== id);
            this.cdr.detectChanges();
            Swal.fire('Deleted!', 'Category removed.', 'success');
          },
          error: (err) => Swal.fire('Error', 'Unauthorized or server error', 'error')
        });
      }
    });
  }

  editCategory(id: number) {
    this.router.navigate(['/dashboard/edit-category', id]);
  }

  addCategory() {
    this.router.navigate(['/dashboard/add-category']);
  }

  // --- Product Logic ---
 loadproducts() {
  this.productservices.getProducts().subscribe({
    next: (data) => {
      this.products = data; 
      this.cdr.detectChanges();
    },
    error: (err) => console.error('Failed to load products', err)
  });
}

  addProduct() {
    this.router.navigate(['/dashboard/add-product']);
  }

  editProduct(id: number) {
    this.router.navigate(['/dashboard/edit-product', id]);
  }

deleteProduct(id: number) {
     Swal.fire({
       title: 'Are you sure?',
       text: "You won't be able to revert this!",
       icon: 'warning',
       showCancelButton: true,
       confirmButtonColor: '#d33',
       confirmButtonText: 'Yes, delete it!'
     }).then((result) => {
       if (result.isConfirmed) {
         this.productservices.deleteProduct(id).subscribe({
           next: () => {
             this.products = this.products.filter((p: any) => p.id !== id);
             this.cdr.detectChanges();
             Swal.fire('Deleted!', 'Product has been deleted.', 'success');
           },
           // إضافة هذا الجزء ضروري جداً لاكتشاف الخطأ
           error: (err) => {
             console.error('Delete Error:', err);
             Swal.fire('Error!', 'Failed to delete product. Check console.', 'error');
           }
         });
       }
     });
   }

  // --- User Logic ---
  loadAllUsers() {
    this.authService.getAllUsers().subscribe({
      next: (data) => {
        this.users = data;
        this.cdr.detectChanges();
      },
      error: () => undefined,
    });
  }

  onDeleteUser(email: string) {
    Swal.fire({
      title: 'Delete User?',
      text: `Are you sure you want to delete ${email}?`,
      icon: 'error',
      showCancelButton: true,
      confirmButtonColor: '#d33'
    }).then((result) => {
      if (result.isConfirmed) {
        this.authService.deleteUser(email).subscribe(() => {
          this.users = this.users.filter(u => u.email !== email);
          this.cdr.detectChanges();
          Swal.fire('Deleted!', 'User removed successfully.', 'success');
        });
      }
    });
  }

  onLogout() {
    this.authService.logout();
  }

  onSearchUser() {
    if (!this.searchEmail.trim()) {
      this.searchResult = null;
      this.loadAllUsers(); 
      return;
    }

    this.authService.getUserByEmail(this.searchEmail).subscribe({
      next: (data) => {
        this.searchResult = data;
        this.users = [data]; 
        this.cdr.detectChanges();
      },
      error: (err) => {
        Swal.fire('Error', 'User not found or invalid email', 'error');
        this.searchResult = null;
      }
    });
  }

  clearSearch() {
    this.searchEmail = '';
    this.searchResult = null;
    if (this.canManageUsers) {
      this.loadAllUsers();
    }
  }
}
