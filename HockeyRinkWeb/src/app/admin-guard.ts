import { Injectable, inject } from "@angular/core";
import { CanActivateFn, Router } from "@angular/router";
import { AuthService } from "./auth";

export const AdminGuard: CanActivateFn = (): boolean => {
  const authService = inject(AuthService);
  const router = inject(Router);

  if (authService.isAdmin()) {
    return true;
  }

  router.navigate(["/home"]);
  return false;
};
