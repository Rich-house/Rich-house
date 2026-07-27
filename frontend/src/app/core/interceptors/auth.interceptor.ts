import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';
import { Auth } from '../services/auth';
import { apiConfig, isApiRequestUrl } from '../config/api.config';

const shouldInvalidateSession = (requestUrl: string, status: number): boolean => {
  if (status === 401) {
    return true;
  }

  return status === 403 && requestUrl === apiConfig.currentUser;
};

export const authInterceptor: HttpInterceptorFn = (request, next) => {
  const authService = inject(Auth);
  const router = inject(Router);
  const accessToken = isApiRequestUrl(request.url) ? authService.getAccessToken() : null;

  const authenticatedRequest = accessToken
    ? request.clone({
        setHeaders: {
          Authorization: `Bearer ${accessToken}`,
        },
      })
    : request;

  return next(authenticatedRequest).pipe(
    catchError((error: unknown) => {
      if (
        error instanceof HttpErrorResponse
        && shouldInvalidateSession(authenticatedRequest.url, error.status)
      ) {
        authService.handleInvalidSession(router.url);
      }

      return throwError(() => error);
    }),
  );
};
