import { Injectable, inject } from "@angular/core";
import { CanActivateFn, Router } from "@angular/router";
import { Observable, of } from "rxjs";
import { catchError, map, take } from "rxjs/operators";
import { AuthService } from "./auth";

export const AuthGuard: CanActivateFn = (): Observable<boolean> => {
  const authService = inject(AuthService);
  const router = inject(Router);

  const token = authService.getToken();
  if (!token) {
    router.navigate(["/login"]);
    return of(false);
  }

  return authService.checkAuthStatus().pipe(
    take(1),
    map((response) => {
      if (response?.isValid) {
        return true;
      }
      router.navigate(["/login"]);
      return false;
    }),
    catchError(() => {
      router.navigate(["/login"]);
      return of(false);
    })
  );
};
