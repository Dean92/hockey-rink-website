import { CommonModule } from "@angular/common";
import { Component, OnInit } from "@angular/core";
import { DataService } from "../data";
import { AuthService } from "../auth";

@Component({
  selector: "app-leagues",
  imports: [CommonModule],
  templateUrl: "./leagues.html",
  styleUrls: ["./leagues.css"],
})
export class Leagues implements OnInit {
  leagues: any[] = [];
  errorMessage: string | null = null;

  constructor(
    private dataService: DataService,
    private authService: AuthService
  ) {}

  ngOnInit() {
    this.checkAuthAndLoadLeagues();
  }

  checkAuthAndLoadLeagues() {
    // First check if we're authenticated
    this.authService.checkAuthStatus().subscribe({
      next: (authStatus) => {
        // Check both token-based and cookie-based auth responses
        // Note: API returns lowercase property names
        const isAuthenticated =
          authStatus.isValid ||
          authStatus.IsAuthenticated ||
          authStatus.isAuthenticated;

        if (isAuthenticated) {
          this.loadLeagues();
        } else {
          this.errorMessage = "Not authenticated. Please login again.";
        }
      },
      error: (err) => {
        console.error("Auth check failed:", err);
        this.errorMessage = "Authentication check failed. Please login again.";
      },
    });
  }

  loadLeagues() {
    this.dataService.getLeagues().subscribe({
      next: (data) => {
        this.leagues = data;
      },
      error: (err) => {
        console.error("Error fetching leagues", err);
        this.errorMessage = `Failed to fetch leagues: ${err.status} ${err.statusText}`;
      },
    });
  }
}
