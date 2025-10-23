import { Component, OnInit, signal } from "@angular/core";
import { CommonModule, DatePipe, CurrencyPipe } from "@angular/common";
import { RouterLink } from "@angular/router";
import { AdminService, AdminDashboardData } from "../admin.service";

@Component({
  selector: "app-admin-dashboard",
  standalone: true,
  imports: [CommonModule, DatePipe, CurrencyPipe, RouterLink],
  templateUrl: "./admin-dashboard.html",
  styleUrls: ["./admin-dashboard.css"],
})
export class AdminDashboard implements OnInit {
  dashboardData = signal<AdminDashboardData | null>(null);
  errorMessage = signal<string | null>(null);
  isLoading = signal<boolean>(true);

  constructor(private adminService: AdminService) {}

  ngOnInit() {
    this.loadDashboard();
  }

  loadDashboard() {
    this.adminService.getDashboard().subscribe({
      next: (data) => {
        console.log("Admin dashboard data loaded:", data);
        this.dashboardData.set(data);
        this.isLoading.set(false);
      },
      error: (err) => {
        console.error("Error fetching admin dashboard:", err);
        this.errorMessage.set(
          err.error?.message || "Failed to load admin dashboard"
        );
        this.isLoading.set(false);
      },
    });
  }
}
