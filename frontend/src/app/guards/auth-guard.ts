import { inject } from '@angular/core';
import { CanActivateFn } from '@angular/router';
import { Auth } from '../Services/auth';

export const authGuard: CanActivateFn = (route, state) => {
  const authService = inject(Auth);

  if (authService.hasValidSession()) {
    return true;
  }

  authService.redirectToLogin(state.url);
  return false;
};
