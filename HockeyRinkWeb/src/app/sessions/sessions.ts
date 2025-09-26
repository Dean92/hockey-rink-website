import { Component, OnInit } from "@angular/core";
import { DataService } from "../data";
import { AuthService } from "../auth";
import { CommonModule, DatePipe } from "@angular/common";

@Component({
  selector: "app-sessions",
  standalone: true,
  imports: [CommonModule, DatePipe],
  templateUrl: "./sessions.html",
  styleUrls: ["./sessions.css"],
})
export class Sessions implements OnInit {
  sessions: any[] = [];
  errorMessage: string | null = null;

  constructor(
    private dataService: DataService,
    private authService: AuthService
  ) {}

  ngOnInit() {
    this.checkAuthAndLoadSessions();
  }

  checkAuthAndLoadSessions() {
    this.authService.checkAuthStatus().subscribe({
      next: (authStatus) => {
        const isAuthenticated =
          authStatus.isValid ||
          authStatus.IsAuthenticated ||
          authStatus.isAuthenticated;

        if (isAuthenticated) {
          this.loadSessions();
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

  loadSessions() {
    this.dataService.getSessions().subscribe({
      next: (data) => {
        console.log("Sessions fetched:", data);
        this.sessions = data;
      },
      error: (err) => {
        console.error("Error fetching sessions:", err);
        this.errorMessage =
          err.error?.message || "Failed to fetch sessions. Please try again.";
      },
    });
  }
}
