import { Component } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { Auth } from '../../Services/auth';

@Component({
  selector: 'app-confirm-email',
  standalone: true,
  imports: [FormsModule, CommonModule],
  templateUrl: './confirmemail.html',
  styleUrls: ['./confirmemail.css']
})
export class ConfirmEmailComponent {

  email: string = '';
  code: string = '';

  message: string = '';
  error: string = '';

  constructor(
    private route: ActivatedRoute,
    private auth: Auth,
    private router: Router
  ) {
    this.route.queryParams.subscribe(params => {
      this.email = params['email'];
    });
  }

  confirm() {
    const data: any = {
      email: this.email,
      code: this.code
    };

    this.auth.confirmEmail(data).subscribe({
      next: () => {
        this.message = "Email confirmed successfully 🎉";

        setTimeout(() => {
          this.router.navigate(['/login']);
        }, 1500);
      },
      error: (err) => {
        this.error = err.error?.message || "Invalid code";
      }
    });
  }
}