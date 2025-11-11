import { Component, OnInit, signal } from "@angular/core";
import { DataService } from "../data";
import { CommonModule, DatePipe } from "@angular/common";
import { RouterLink } from "@angular/router";
import { DashboardData } from "../models";

@Component({
  selector: "app-dashboard",
  standalone: true,
  imports: [CommonModule, DatePipe, RouterLink],
  templateUrl: "./dashboard.html",
  styleUrls: ["./dashboard.css"],
})
export class Dashboard implements OnInit {
  dashboardData = signal<DashboardData | null>(null);
  errorMessage = signal<string | null>(null);
  isLoading = signal<boolean>(true);

  constructor(private dataService: DataService) {}

  ngOnInit() {
    this.loadDashboard();
  }

  loadDashboard() {
    this.dataService.getDashboard().subscribe({
      next: (data) => {
        console.log("Dashboard data loaded:", data);
        this.dashboardData.set(data);
        this.isLoading.set(false);
      },
      error: (err) => {
        console.error("Error fetching dashboard:", err);
        this.errorMessage.set(
          err.error?.message || "Failed to fetch dashboard. Please try again."
        );
        this.isLoading.set(false);
      },
    });
  }
}
