import { Component, OnInit } from "@angular/core";
import { DataService } from "../data";
import { AuthService } from "../auth";
import { Router } from "@angular/router";
import { CommonModule } from "@angular/common";

@Component({
  selector: "app-profile",
  imports: [CommonModule],
  templateUrl: "./profile.html",
  styleUrl: "./profile.css",
})
export class Profile implements OnInit {
  profile: any;
  errorMessage: string | null = null;

  constructor(
    private dataService: DataService,
    private authService: AuthService,
    private router: Router
  ) {}

  ngOnInit() {
    this.checkAuthAndLoadProfile();
  }

  checkAuthAndLoadProfile() {
    this.authService.checkAuthStatus().subscribe({
      next: (authStatus) => {
        const isAuthenticated =
          authStatus.isValid ||
          authStatus.IsAuthenticated ||
          authStatus.isAuthenticated;

        if (isAuthenticated) {
          this.loadProfile();
        } else {
          this.errorMessage = "Not authenticated. Please login again.";
          this.router.navigate(["/login"]);
        }
      },
      error: (err) => {
        console.error("Auth check failed:", err);
        this.errorMessage = "Authentication check failed. Please login again.";
        this.router.navigate(["/login"]);
      },
    });
  }

  loadProfile() {
    this.dataService.getProfile().subscribe({
      next: (data) => {
        console.log("Profile fetched:", data);
        this.profile = data;
      },
      error: (err) => {
        console.error("Error fetching profile:", err);
        this.errorMessage =
          err.error?.message || "Failed to load profile. Please try again.";
      },
    });
  }
}
