import { Component, OnInit, signal, computed } from '@angular/core';
import { DataService } from '../data';
import { CommonModule, DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { ToastService } from '../services/toast.service';
import { ToastContainerComponent } from '../toast-container/toast-container.component';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, DatePipe, RouterLink, ToastContainerComponent],
  templateUrl: './dashboard.html',
  styleUrls: ['./dashboard.css'],
})
export class Dashboard implements OnInit {
  upcomingSessions = signal<any[]>([]);
  currentSessions = signal<any[]>([]);
  pastSessions = signal<any[]>([]);
  userProfile = signal<any>(null);
  errorMessage = signal<string | null>(null);
  isLoading = signal<boolean>(true);
  sessionToCancel = signal<any>(null);
  currentTeam = signal<any>(null);

  totalSessionsCount = computed(() => {
    return (
      this.upcomingSessions().length +
      this.currentSessions().length +
      this.pastSessions().length
    );
  });

  constructor(
    private dataService: DataService,
    private toastService: ToastService
  ) {}

  ngOnInit() {
    this.loadSessions();
    this.loadProfile();
    this.loadCurrentTeam();
  }

  loadProfile() {
    this.dataService.getProfile().subscribe({
      next: (data) => {
        this.userProfile.set(data);
      },
      error: (err) => {
        console.error('Error fetching profile:', err);
      },
    });
  }

  loadCurrentTeam() {
    this.dataService.getMyTeams().subscribe({
      next: (teams) => {
        if (teams && teams.length > 0) {
          // Get the most recent team (teams are ordered by date descending)
          const latestTeam = teams[0];
          this.currentTeam.set(latestTeam);
        }
      },
      error: (err) => {
        // Silently fail - will show "No team assigned" in UI
        // This can happen if user hasn't been assigned to any teams yet
        // or if there's a temporary auth issue
        console.log('No teams found or unable to load teams');
      },
    });
  }

  loadSessions() {
    this.isLoading.set(true);
    this.dataService.getMySessions().subscribe({
      next: (data) => {
        console.log('Sessions data loaded:', data);

        // Convert UTC dates to proper Date objects
        const convertDates = (sessions: any[]) => {
          return sessions.map((session) => ({
            ...session,
            startDate: session.startDate
              ? new Date(session.startDate + 'Z')
              : null,
            endDate: session.endDate ? new Date(session.endDate + 'Z') : null,
            registrationDate: session.registrationDate
              ? new Date(session.registrationDate + 'Z')
              : null,
          }));
        };

        this.upcomingSessions.set(convertDates(data.upcomingSessions || []));
        this.currentSessions.set(convertDates(data.currentSessions || []));
        this.pastSessions.set(convertDates(data.pastSessions || []));
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

  confirmCancel(session: any) {
    this.sessionToCancel.set(session);
  }

  cancelConfirm() {
    const session = this.sessionToCancel();
    if (!session) return;

    this.dataService
      .cancelSessionRegistration(session.registrationId)
      .subscribe({
        next: (response) => {
          this.toastService.success(
            'Cancellation Request Submitted',
            'Your cancellation request has been sent to the admin. Refunds will be processed within 7-14 business days.'
          );
          this.sessionToCancel.set(null);
          this.loadSessions(); // Reload sessions
        },
        error: (err) => {
          console.error('Error cancelling registration:', err);
          this.toastService.error(
            'Cancellation Failed',
            err.error?.message || 'Failed to cancel registration'
          );
          this.sessionToCancel.set(null);
        },
      });
  }

  cancelDismiss() {
    this.sessionToCancel.set(null);
  }

  canCancelSession(session: any): boolean {
    if (!session.startDate) return false;

    const startDate = new Date(session.startDate);
    const now = new Date();
    const daysUntilStart =
      (startDate.getTime() - now.getTime()) / (1000 * 60 * 60 * 24);

    return daysUntilStart >= 7;
  }
}
