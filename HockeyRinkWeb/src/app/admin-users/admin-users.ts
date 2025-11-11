import { Component, OnInit, signal } from "@angular/core";
import { CommonModule, DatePipe } from "@angular/common";
import { AdminService, AdminUser } from "../admin.service";

@Component({
  selector: "app-admin-users",
  standalone: true,
  imports: [CommonModule, DatePipe],
  templateUrl: "./admin-users.html",
  styleUrls: ["./admin-users.css"],
})
export class AdminUsers implements OnInit {
  users = signal<AdminUser[]>([]);
  errorMessage = signal<string | null>(null);
  isLoading = signal<boolean>(true);

  constructor(private adminService: AdminService) {}

  ngOnInit() {
    this.loadUsers();
  }

  loadUsers() {
    this.adminService.getUsers().subscribe({
      next: (data) => {
        console.log("Users loaded:", data);
        this.users.set(data);
        this.isLoading.set(false);
      },
      error: (err) => {
        console.error("Error fetching users:", err);
        this.errorMessage.set(err.error?.message || "Failed to load users");
        this.isLoading.set(false);
      },
    });
  }
}
