import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { UserService } from '../../Services/user-service';
import { CommonModule } from '@angular/common';
import { describeApiError } from '../../core/utils/http-error';

@Component({
  selector: 'app-profile',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './profile.html',
  styleUrl: './profile.css',
})
export class Profile implements OnInit {
  userData: any;
  isLoading: boolean = true;
  errorTitle: string = '';
  errorMessage: string = '';

  constructor(
    private _userService: UserService,
    private cdr: ChangeDetectorRef 
  ) {}

  ngOnInit(): void {
    this.getUserData();
  }

  getUserData(forceRefresh = false) {
    this.isLoading = true;
    this.errorTitle = '';
    this.errorMessage = '';
    this._userService.getUserProfile(forceRefresh).subscribe({
      next: (data) => {
        this.userData = data;
        this.isLoading = false;
        this.cdr.detectChanges();
      },
      error: (err) => {
        const apiError = describeApiError(err, {
          unavailable:
            'The Rich House backend is unavailable, so your profile could not be loaded yet.',
          server:
            'The profile service returned an unexpected error. Please try again shortly.',
          unknown: 'Failed to load profile data.',
        });

        if (apiError.kind === 'unauthorized') {
          this.isLoading = false;
          return;
        }

        this.userData = null;
        this.errorTitle = apiError.title;
        this.errorMessage = apiError.message;
        this.isLoading = false;
        this.cdr.detectChanges();
      },
    });
  }

  retry(): void {
    this.getUserData(true);
  }
}
