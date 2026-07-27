import { Component, ChangeDetectorRef } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { Auth } from '../../Services/auth';
import { FormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { ProductsService } from '../../Services/product';
import { describeApiError } from '../../core/utils/http-error';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [FormsModule, CommonModule],
  templateUrl: './login.html',
  styleUrls: ['./login.css']
})
export class LoginComponent {

  loginData = { Email: '', Password: '' };

  emailError: string = '';
  passwordError: string = '';
  generalError: string = '';

  isLoading: boolean = false;

  constructor(
    private authService: Auth, 
    private router: Router,
    private route: ActivatedRoute,
    private productService: ProductsService,
    private cdr: ChangeDetectorRef 
  ) {}

  onLogin() {
    this.emailError = '';
    this.passwordError = '';
    this.generalError = '';
    this.isLoading = true;

    this.authService.login(this.loginData).subscribe({
      next: (response: any) => {
        this.isLoading = false;
        
        if (response && response.description && !response.token) {
           this.generalError = response.description;
           this.cdr.detectChanges(); 
           return;
        }

        if (response && response.token) {
            this.authService.storeSession(response);
            this.productService.updateCartCount();

            const returnUrl = this.route.snapshot.queryParamMap.get('returnUrl');
            const fallbackRoute = this.authService.isManagementUser() ? '/dashboard' : '/shop';
            this.router.navigateByUrl(returnUrl || fallbackRoute);
        } else {
            this.generalError = "Unexpected error occurred.";
        }
        
        this.cdr.detectChanges(); 
      },

      error: (err: any) => {
        this.isLoading = false;

        let message = 'Login failed. Please try again.';

        if (describeApiError(err).kind === 'unavailable') {
          message = 'The Rich House backend is unavailable. Start the API and try again.';
        } else if (err?.error?.description) {
          message = err.error.description;
        } else if (err?.error?.message) {
          message = err.error.message;
        } else if (typeof err?.error === 'string') {
          message = err.error;
        } else if (err?.message) {
          message = err.message;
        }

        this.generalError = message;
        this.cdr.detectChanges(); 
      },
      
      complete: () => {
         this.isLoading = false;
         this.cdr.detectChanges();
      }
    });
  }
}
