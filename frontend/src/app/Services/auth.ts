import { HttpClient } from '@angular/common/http';
import { Injectable, signal } from '@angular/core';
import { Router } from '@angular/router';
import { Observable } from 'rxjs';
import { jwtDecode } from 'jwt-decode';
import { apiConfig } from '../core/config/api.config';

interface AuthResponsePayload {
  email?: string | null;
  firstName?: string | null;
  lastName?: string | null;
  token: string;
}

interface JwtPayload {
  exp?: number;
  role?: string;
  'http://schemas.microsoft.com/ws/2008/06/identity/claims/role'?: string;
}

const authStorageKeys = ['token', 'firstName', 'lastName', 'email', 'Email'] as const;
const invalidStoredValues = new Set(['', 'null', 'undefined']);

@Injectable({
  providedIn: 'root',
})
export class Auth {
  readonly isLoggedIn = signal<boolean>(false);
  private redirectInProgress = false;

  constructor(
    private http: HttpClient,
    private router: Router,
  ) {
    this.isLoggedIn.set(!!this.readValidStoredToken());
  }

  login(credentials: any): Observable<AuthResponsePayload> {
    return this.http.post<AuthResponsePayload>(`${apiConfig.auth}/login`, credentials);
  }

  register(data: any): Observable<any> {
    return this.http.post(`${apiConfig.auth}/register`, data);
  }

  confirmEmail(data: any): Observable<any> {
    return this.http.post(`${apiConfig.auth}/confirm-email`, data);
  }

  storeSession(response: AuthResponsePayload): void {
    this.clearAuthStorage(false);
    localStorage.setItem('token', response.token);
    localStorage.setItem('firstName', response.firstName ?? '');
    localStorage.setItem('lastName', response.lastName ?? '');
    localStorage.setItem('email', response.email ?? '');
    this.isLoggedIn.set(true);
  }

  logout(returnUrl?: string): void {
    this.clearAuthStorage();
    this.redirectToLogin(returnUrl);
  }

  redirectToLogin(returnUrl?: string): void {
    if (this.redirectInProgress) {
      return;
    }

    this.redirectInProgress = true;
    const normalizedReturnUrl =
      returnUrl && returnUrl !== '/login' && returnUrl !== '/register' ? returnUrl : undefined;

    void this.router
      .navigate(['/login'], {
        queryParams: normalizedReturnUrl ? { returnUrl: normalizedReturnUrl } : undefined,
        replaceUrl: true,
      })
      .finally(() => {
        this.redirectInProgress = false;
      });
  }

  handleInvalidSession(returnUrl?: string): void {
    this.clearAuthStorage();
    this.redirectToLogin(returnUrl);
  }

  hasValidSession(): boolean {
    const token = this.readValidStoredToken();
    this.isLoggedIn.set(!!token);
    return !!token;
  }

  getAccessToken(): string | null {
    const token = this.readValidStoredToken();
    this.isLoggedIn.set(!!token);
    return token;
  }

  getAllUsers() {
    return this.http.get<any[]>(`${apiConfig.admin}/GetAllUsers`);
  }

  getUserByEmail(email: string) {
    return this.http.get<any>(`${apiConfig.admin}/GetUserByEmail?email=${email}`);
  }

  deleteUser(email: string) {
    return this.http.delete(`${apiConfig.admin}/DeleteUserByEmail?email=${email}`);
  }

  getUserRole(token = this.getAccessToken()): string | null {
    if (!token) {
      return null;
    }

    try {
      const decodedToken = jwtDecode<JwtPayload>(token);
      const role =
        decodedToken.role
        || decodedToken['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'];

      return role || null;
    } catch {
      return null;
    }
  }

  isAdmin(): boolean {
    return this.getUserRole() === 'SuperAdmin';
  }

  isSuperAdmin(): boolean {
    return this.getUserRole() === 'SuperAdmin';
  }

  isManagementUser(): boolean {
    const role = this.getUserRole();
    return role === 'Admin' || role === 'SuperAdmin';
  }

  private readValidStoredToken(): string | null {
    const token = this.readStoredValue('token');
    if (!token) {
      return null;
    }

    if (this.isTokenExpired(token)) {
      this.clearAuthStorage();
      return null;
    }

    return token;
  }

  private readStoredValue(key: (typeof authStorageKeys)[number]): string | null {
    const value = localStorage.getItem(key) ?? sessionStorage.getItem(key);
    return value && !invalidStoredValues.has(value) ? value : null;
  }

  private isTokenExpired(token: string): boolean {
    try {
      const decodedToken = jwtDecode<JwtPayload>(token);
      return !!decodedToken.exp && decodedToken.exp * 1000 <= Date.now();
    } catch {
      return true;
    }
  }

  private clearAuthStorage(updateSignal = true): void {
    authStorageKeys.forEach((key) => {
      localStorage.removeItem(key);
      sessionStorage.removeItem(key);
    });

    if (updateSignal) {
      this.isLoggedIn.set(false);
    }
  }
}
