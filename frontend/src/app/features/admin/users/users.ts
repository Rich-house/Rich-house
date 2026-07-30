import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import Swal from 'sweetalert2';
import { Auth } from '../../../core/services/auth';
import { describeApiError } from '../../../core/utils/http-error';

interface AdminUser {
  email: string;
}

@Component({
  selector: 'app-admin-users',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './users.html',
  styleUrl: './users.css',
})
export class AdminUsersComponent {
  private readonly authService = inject(Auth);
  private readonly cdr = inject(ChangeDetectorRef);

  users: AdminUser[] = [];
  loading = true;
  errorMessage = '';
  searchEmail = '';

  constructor() {
    this.loadUsers();
  }

  loadUsers(): void {
    this.loading = true;
    this.errorMessage = '';

    this.authService.getAllUsers().subscribe({
      next: (users) => {
        this.users = users;
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

  searchUsers(): void {
    const email = this.searchEmail.trim();
    if (!email) {
      this.loadUsers();
      return;
    }

    this.loading = true;
    this.authService.getUserByEmail(email).subscribe({
      next: (user) => {
        this.users = user ? [user] : [];
        this.loading = false;
        this.cdr.markForCheck();
      },
      error: (error) => {
        this.loading = false;
        this.cdr.markForCheck();
        const message = error?.error?.message || describeApiError(error).message;
        void Swal.fire('User lookup failed', message, 'error');
      },
    });
  }

  resetSearch(): void {
    this.searchEmail = '';
    this.loadUsers();
  }

  deleteUser(user: AdminUser): void {
    void Swal.fire({
      title: `Delete ${user.email}?`,
      text: 'This administrator account will be removed permanently.',
      icon: 'warning',
      showCancelButton: true,
      confirmButtonText: 'Delete user',
      confirmButtonColor: '#b08a44',
    }).then((result) => {
      if (!result.isConfirmed) {
        return;
      }

      this.authService.deleteUser(user.email).subscribe({
        next: () => {
          this.users = this.users.filter((item) => item.email !== user.email);
          this.cdr.markForCheck();
          void Swal.fire('Deleted', 'The user was removed successfully.', 'success');
        },
        error: (error) => {
          const message = error?.error?.message || describeApiError(error).message;
          void Swal.fire('Delete failed', message, 'error');
        },
      });
    });
  }

  trackByEmail(_index: number, user: AdminUser): string {
    return user.email;
  }
}
