import { CommonModule } from "@angular/common";
import { Component, OnInit, signal } from "@angular/core";
import { RouterLink } from "@angular/router";
import { DataService } from "../data";
import { League } from "../models";
import { AuthService } from "../auth";

@Component({
  selector: "app-leagues",
  imports: [CommonModule, RouterLink],
  templateUrl: "./leagues.html",
  styleUrls: ["./leagues.css"],
})
export class Leagues implements OnInit {
  leagues = signal<League[]>([]);
  errorMessage = signal<string | null>(null);
  isLoading = signal<boolean>(false);

  constructor(
    private dataService: DataService,
    protected authService: AuthService
  ) {}

  ngOnInit() {
    this.loadLeagues();
  }

  loadLeagues() {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    this.dataService.getLeagues().subscribe({
      next: (data) => {
        this.leagues.set(data);
        this.isLoading.set(false);
      },
      error: (err) => {
        console.error("Error fetching leagues", err);
        this.errorMessage.set(
          "Failed to load leagues. Please try again later."
        );
        this.isLoading.set(false);
      },
    });
  }

  formatDate(dateString?: string): string {
    if (!dateString) return "TBA";
    return new Date(dateString).toLocaleDateString("en-US", {
      month: "long",
      year: "numeric",
    });
  }

  isAuthenticated(): boolean {
    return this.authService.getToken() !== null;
  }

  isRegistrationOpen(league: League): boolean {
    // Check if expectedStartDate exists and is in the future
    if (!league.expectedStartDate) return false;
    const startDate = new Date(league.expectedStartDate);
    const today = new Date();
    return startDate > today;
  }
}
