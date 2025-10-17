import { Injectable } from "@angular/core";
import { CanActivate, Router } from "@angular/router";
import { Observable, map, take } from "rxjs";
import { AuthService } from "./auth";

@Injectable({
  providedIn: "root",
})
export class AuthGuard implements CanActivate {
  constructor(private authService: AuthService, private router: Router) {}

  canActivate(): Observable<boolean> {
    return this.authService.checkAuthStatus().pipe(
      take(1),
      map((response) => {
        if (response && response.isAuthenticated) {
          return true;
        } else {
          // Redirect to login page if not authenticated
          this.router.navigate(["/login"]);
          return false;
        }
      })
    );
  }
}
