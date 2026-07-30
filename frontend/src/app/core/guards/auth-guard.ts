import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { Auth } from '../services/auth';

export const authGuard: CanActivateFn = (route, state) => {
  const authService = inject(Auth);
  const router = inject(Router);

  if (authService.hasValidSession()) {
    const requiresManagementAccess = route.data['managementOnly'] === true;
    const requiresSuperAdmin = route.data['superAdminOnly'] === true;
    if (requiresManagementAccess && !authService.isManagementUser()) {
      return router.parseUrl('/shop');
    }

    if (requiresSuperAdmin && !authService.isSuperAdmin()) {
      return router.parseUrl('/dashboard');
    }

    return true;
  }

  authService.redirectToLogin(state.url);
  return false;
};
