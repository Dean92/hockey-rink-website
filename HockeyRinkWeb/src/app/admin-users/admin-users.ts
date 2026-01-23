import { Component, OnInit, signal, computed, effect } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AdminService, AdminUser } from '../admin.service';

@Component({
  selector: 'app-admin-users',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './admin-users.html',
  styleUrls: ['./admin-users.css'],
})
export class AdminUsers implements OnInit {
  users = signal<AdminUser[]>([]);
  errorMessage = signal<string | null>(null);
  isLoading = signal<boolean>(true);
  searchTerm = signal<string>('');
  currentPage = signal<number>(1);
  pageSize = 25;
  Math = Math; // For template access

  filteredUsers = computed(() => {
    const term = this.searchTerm().toLowerCase().trim();
    if (!term) return this.users();

    return this.users().filter(
      (user) =>
        user.firstName.toLowerCase().includes(term) ||
        user.lastName.toLowerCase().includes(term)
    );
  });

  paginatedUsers = computed(() => {
    const filtered = this.filteredUsers();
    const start = (this.currentPage() - 1) * this.pageSize;
    const end = start + this.pageSize;
    return filtered.slice(start, end);
  });

  totalPages = computed(() => {
    return Math.ceil(this.filteredUsers().length / this.pageSize);
  });

  constructor(private adminService: AdminService, private router: Router) {
    // Reset to page 1 when search term changes
    effect(() => {
      this.searchTerm(); // Track searchTerm changes
      this.currentPage.set(1); // Reset to first page
    });
  }

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

  viewUserProfile(userId: string) {
    this.router.navigate(['/profile'], { queryParams: { userId } });
  }

  goToPage(page: number) {
    if (page >= 1 && page <= this.totalPages()) {
      this.currentPage.set(page);
    }
  }

  nextPage() {
    if (this.currentPage() < this.totalPages()) {
      this.currentPage.set(this.currentPage() + 1);
    }
  }

  previousPage() {
    if (this.currentPage() > 1) {
      this.currentPage.set(this.currentPage() - 1);
    }
  }
}
