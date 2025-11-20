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
      day: "numeric",
      year: "numeric",
    });
  }

  formatDateTime(dateString?: string): string {
    if (!dateString) return "TBA";
    return new Date(dateString).toLocaleDateString("en-US", {
      month: "short",
      day: "numeric",
      year: "numeric",
      hour: "numeric",
      minute: "2-digit",
    });
  }

  isAuthenticated(): boolean {
    return this.authService.getToken() !== null;
  }

  isRegistrationOpen(league: League): boolean {
    const now = new Date();

    // Check if registration open/close dates are set
    if (league.registrationOpenDate && league.registrationCloseDate) {
      const openDate = new Date(league.registrationOpenDate);
      const closeDate = new Date(league.registrationCloseDate);
      return now >= openDate && now <= closeDate;
    }

    // Fallback to legacy expectedStartDate check
    if (league.expectedStartDate) {
      const startDate = new Date(league.expectedStartDate);
      return startDate > now;
    }

    return false;
  }

  getCurrentPrice(league: League): number | null {
    const now = new Date();

    // Check if early bird pricing is active
    if (league.earlyBirdPrice && league.earlyBirdEndDate) {
      const earlyBirdEnd = new Date(league.earlyBirdEndDate);
      if (now <= earlyBirdEnd) {
        return league.earlyBirdPrice;
      }
    }

    // Return regular price if available
    if (league.regularPrice) {
      return league.regularPrice;
    }

    // Fallback to legacy field
    if (league.preRegisterPrice) {
      return league.preRegisterPrice;
    }

    // If early bird price exists but no end date, show it
    if (league.earlyBirdPrice) {
      return league.earlyBirdPrice;
    }

    return null;
  }

  getPriceLabel(league: League): string {
    const now = new Date();

    // Check if early bird pricing is active
    if (league.earlyBirdPrice && league.earlyBirdEndDate) {
      const earlyBirdEnd = new Date(league.earlyBirdEndDate);
      if (now <= earlyBirdEnd) {
        return "Early Bird Registration";
      }
    }

    return "Registration Fee";
  }
}
