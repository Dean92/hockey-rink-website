import { Component, OnInit } from "@angular/core";
import { DataService } from "../data";
import { CommonModule, DatePipe } from "@angular/common";

@Component({
  selector: "app-sessions",
  imports: [CommonModule, DatePipe],
  templateUrl: "./sessions.html",
  styleUrls: ["./sessions.css"],
})
export class Sessions implements OnInit {
  sessions: any[] = [];
  errorMessage: string | null = null;

  constructor(private dataService: DataService) {}

  ngOnInit() {
    this.loadSessions();
  }

  loadSessions() {
    this.dataService.getSessions().subscribe({
      next: (data) => {
        this.sessions = data;
      },
      error: (err) => {
        console.error("Error fetching sessions", err);
        this.errorMessage = "Failed to fetch sessions";
      },
    });
  }
}
