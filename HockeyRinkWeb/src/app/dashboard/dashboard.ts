import { Component, OnInit, signal } from '@angular/core';
import { DataService } from '../data';
import { CommonModule, DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, DatePipe, RouterLink],
  templateUrl: './dashboard.html',
  styleUrls: ['./dashboard.css'],
})
export class Dashboard implements OnInit {
  upcomingSessions = signal<any[]>([]);
  currentSessions = signal<any[]>([]);
  pastSessions = signal<any[]>([]);
  errorMessage = signal<string | null>(null);
  isLoading = signal<boolean>(true);

  constructor(private dataService: DataService) {}

  ngOnInit() {
    this.loadSessions();
  }

  loadSessions() {
    this.isLoading.set(true);
    this.dataService.getMySessions().subscribe({
      next: (data) => {
        console.log('Sessions data loaded:', data);
        this.upcomingSessions.set(data.upcomingSessions || []);
        this.currentSessions.set(data.currentSessions || []);
        this.pastSessions.set(data.pastSessions || []);
        this.isLoading.set(false);
      },
      error: (err) => {
        console.error('Error fetching sessions:', err);
        this.errorMessage.set(
          err.error?.message || 'Failed to fetch sessions. Please try again.'
        );
        this.isLoading.set(false);
      },
    });
  }
}
