import { Component, OnInit, signal, inject } from '@angular/core';
import { CommonModule, DatePipe, CurrencyPipe } from '@angular/common';
import { RouterLink, ActivatedRoute } from '@angular/router';
import { AdminService, AdminDashboardData } from '../admin.service';
import { AuthService } from '../auth';

@Component({
  selector: 'app-admin-dashboard',
  standalone: true,
  imports: [CommonModule, DatePipe, CurrencyPipe, RouterLink],
  templateUrl: './admin-dashboard.html',
  styleUrls: ['./admin-dashboard.css'],
})
export class AdminDashboard implements OnInit {
  dashboardData = signal<AdminDashboardData | null>(null);
  errorMessage = signal<string | null>(null);
  isLoading = signal<boolean>(true);
  accessDenied = signal<boolean>(false);

  private adminService = inject(AdminService);
  private authService = inject(AuthService);
  private route = inject(ActivatedRoute);

  isFullAdmin(): boolean {
    return this.authService.isAdmin();
  }
  canManageRegistrations(): boolean {
    return this.authService.hasPermission('manage-registrations');
  }
  canManageLeagues(): boolean {
    return this.authService.hasPermission('manage-leagues');
  }
  canManageSchedule(): boolean {
    return this.authService.hasPermission('manage-schedule');
  }

  ngOnInit() {
    this.route.queryParams.subscribe((params) => {
      if (params['accessDenied']) {
        this.accessDenied.set(true);
      }
    });
    this.loadDashboard();
  }

  loadDashboard() {
    this.adminService.getDashboard().subscribe({
      next: (data) => {
        console.log('Admin dashboard data loaded:', data);
        this.dashboardData.set(data);
        this.isLoading.set(false);
      },
      error: (err) => {
        console.error('Error fetching admin dashboard:', err);
        this.errorMessage.set(
          err.error?.message || 'Failed to load admin dashboard',
        );
        this.isLoading.set(false);
      },
    });
  }

  formatDateInCentralTime(dateString: string): string {
    // Ensure the date string is treated as UTC
    const utcDate = dateString.endsWith('Z') ? dateString : dateString + 'Z';
    const date = new Date(utcDate);
    // Convert to user's local timezone
    return date.toLocaleString('en-US', {
      month: 'numeric',
      day: 'numeric',
      year: '2-digit',
      hour: 'numeric',
      minute: '2-digit',
      hour12: true,
    });
  }

  getCurrentMonthYear(): string {
    const now = new Date();
    return now.toLocaleString('en-US', {
      month: 'long',
      year: 'numeric',
    });
  }
}
