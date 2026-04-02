import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from './auth';

/**
 * Factory that produces a route guard allowing access if the user is a full
 * Admin or a SubAdmin who holds the specified permission.
 * Redirects to /admin (dashboard) with an access-denied flag when blocked.
 */
export const PermissionGuard =
  (permission: string): CanActivateFn =>
  (): boolean => {
    const authService = inject(AuthService);
    const router = inject(Router);

    if (authService.hasPermission(permission)) {
      return true;
    }

    router.navigate(['/admin'], { queryParams: { accessDenied: true } });
    return false;
  };
