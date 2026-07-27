import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, shareReplay, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { apiConfig } from '../config/api.config';
@Injectable({
  providedIn: 'root',
})
export class UserService {
  private profileRequest$?: Observable<any>;
  private profileRequestToken?: string | null;

  constructor(private http: HttpClient) {}

  getUserProfile(forceRefresh = false): Observable<any> {
    const activeToken = localStorage.getItem('token') ?? sessionStorage.getItem('token');

    if (
      forceRefresh
      || !this.profileRequest$
      || this.profileRequestToken !== activeToken
    ) {
      this.profileRequestToken = activeToken;
      this.profileRequest$ = this.http.get<any>(apiConfig.currentUser).pipe(
        shareReplay({ bufferSize: 1, refCount: false }),
        catchError((error) => {
          this.clearProfileCache();
          return throwError(() => error);
        }),
      );
    }

    return this.profileRequest$;
  }

  clearProfileCache(): void {
    this.profileRequest$ = undefined;
    this.profileRequestToken = undefined;
  }
}
