import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { Auth } from '../../../core/services/auth';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './register.html',
  styleUrls: ['./register.css']
})
export class RegisterComponent {
  registerData = {
    firstName: '',
    lastName: '',
    email: '',
    address: '',
    password: '',
    phoneNumber: '' 
  };

  confirmPassword = '';
  errorMessage: string = ''; 
  successMessage: string = '';

  constructor(private auth: Auth, private router: Router) {}

  register() {
    this.errorMessage = '';
    this.successMessage = '';

    this.auth.register(this.registerData).subscribe({
      next: (response: any) => {
        this.successMessage = 'Registration successful! Redirecting...';
        
        if (response && response.email) {
          localStorage.setItem('Email', response.email);
        }

        setTimeout(() => {
          this.router.navigate(['/confirmemail'], {
            queryParams: { email: this.registerData.email }
          });
        }, 2000);
      },
      error: (err) => {
        this.errorMessage = err.error?.message || 'Something went wrong. Please try again.';
        console.error('Registration error details:', err.error);
      }
    });
  }
}
