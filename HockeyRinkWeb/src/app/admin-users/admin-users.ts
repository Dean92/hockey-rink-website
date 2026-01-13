import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AdminService, AdminUser } from '../admin.service';

@Component({
  selector: 'app-admin-users',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './admin-users.html',
  styleUrls: ['./admin-users.css'],
})
export class AdminUsers implements OnInit {
  users = signal<AdminUser[]>([]);
  errorMessage = signal<string | null>(null);
  isLoading = signal<boolean>(true);

  constructor(private adminService: AdminService) {}

  ngOnInit() {
    this.loadUsers();
  }

  formatDateInCentralTime(dateString: string | Date): string {
    // Convert Date to string if needed
    const dateStr =
      typeof dateString === 'string' ? dateString : dateString.toISOString();
    // Ensure the date string is treated as UTC
    const utcDate = dateStr.endsWith('Z') ? dateStr : dateStr + 'Z';
    const date = new Date(utcDate);
    return date.toLocaleString('en-US', {
      month: 'numeric',
      day: 'numeric',
      year: '2-digit',
      hour: 'numeric',
      minute: '2-digit',
      hour12: true,
    });
  }

  loadUsers() {
    this.adminService.getUsers().subscribe({
      next: (data) => {
        console.log('Users loaded:', data);
        this.users.set(data);
        this.isLoading.set(false);
      },
      error: (err) => {
        console.error('Error fetching users:', err);
        this.errorMessage.set(err.error?.message || 'Failed to load users');
        this.isLoading.set(false);
      },
    });
  }
}
