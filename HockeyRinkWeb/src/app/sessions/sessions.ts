import { Component, OnInit, signal, computed } from '@angular/core';
import { DataService } from '../data';
import { CommonModule, DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { tap, catchError, of } from 'rxjs';
import { Session } from '../models';

@Component({
  selector: 'app-sessions',
  standalone: true,
  imports: [CommonModule, DatePipe, RouterLink],
  templateUrl: './sessions.html',
  styleUrls: ['./sessions.css'],
})
export class Sessions implements OnInit {
  sessions = signal<Session[]>([]);
  userProfile = signal<any>(null);
  errorMessage = signal<string | null>(null);
  isLoading = signal<boolean>(true);

  // Pastel color palette for session headers
  private headerColors = [
    'linear-gradient(135deg, #e3f2fd 0%, #bbdefb 100%)', // Light Blue
    'linear-gradient(135deg, #e1f5fe 0%, #b3e5fc 100%)', // Cyan Blue
    'linear-gradient(135deg, #e0f7fa 0%, #b2ebf2 100%)', // Aqua Blue
    'linear-gradient(135deg, #e8f5e9 0%, #c8e6c9 100%)', // Blue-Green
    'linear-gradient(135deg, #f1f8e9 0%, #dcedc8 100%)', // Light Blue-Green
    'linear-gradient(135deg, #e0f2f1 0%, #b2dfdb 100%)', // Teal Blue
    'linear-gradient(135deg, #e8eaf6 0%, #c5cae9 100%)', // Periwinkle Blue
    'linear-gradient(135deg, #ede7f6 0%, #d1c4e9 100%)', // Lavender Blue
  ];

  constructor(private dataService: DataService) {}

  formatTime(timeString: string): string {
    // timeString comes as "HH:MM:SS" or "HH:MM:SS.ffffff"
    const parts = timeString.split(':');
    if (parts.length >= 2) {
      const hours = parseInt(parts[0], 10);
      const minutes = parts[1];
      const period = hours >= 12 ? 'PM' : 'AM';
      const displayHours = hours % 12 || 12;
      return `${displayHours}:${minutes} ${period}`;
    }
    return timeString;
  }

  isEarlyBirdActive(session: Session): boolean {
    if (!session.earlyBirdPrice || !session.earlyBirdEndDate) {
      return false;
    }
    const now = new Date();
    const earlyBirdEnd = new Date(session.earlyBirdEndDate);
    const registrationOpen = session.registrationOpenDate
      ? new Date(session.registrationOpenDate)
      : null;

    const isWithinEarlyBirdPeriod = earlyBirdEnd > now;
    const isRegistrationOpen = !registrationOpen || registrationOpen <= now;

    return isWithinEarlyBirdPeriod && isRegistrationOpen;
  }

  isRegistrationOpen(session: Session): boolean {
    const now = new Date();
    const registrationOpen = session.registrationOpenDate
      ? new Date(session.registrationOpenDate)
      : null;
    const registrationClose = session.registrationCloseDate
      ? new Date(session.registrationCloseDate)
      : null;

    const hasOpened = !registrationOpen || registrationOpen <= now;
    const hasNotClosed = !registrationClose || registrationClose > now;

    return hasOpened && hasNotClosed;
  }

  isRegistrationOpeningSoon(session: Session): boolean {
    const now = new Date();
    const registrationOpen = session.registrationOpenDate
      ? new Date(session.registrationOpenDate)
      : null;

    if (registrationOpen) {
      console.log(`[${session.name}] Opening Soon Check:`, {
        registrationOpenString: session.registrationOpenDate,
        registrationOpenDate: registrationOpen.toString(),
        registrationOpenTime: registrationOpen.getTime(),
        nowDate: now.toString(),
        nowTime: now.getTime(),
        isInFuture: registrationOpen > now,
        timeDiff: registrationOpen.getTime() - now.getTime(),
      });
    }

    // Check if registration has a future open date
    return !!registrationOpen && registrationOpen > now;
  }

  isSessionStartingSoon(session: Session): boolean {
    const now = new Date();
    const registrationClose = session.registrationCloseDate
      ? new Date(session.registrationCloseDate)
      : null;
    const sessionStart = new Date(session.startDate);

    // Check if registration is closed but session hasn't started yet
    const isRegistrationClosed =
      !!registrationClose && registrationClose <= now;
    const hasNotStarted = sessionStart > now;

    return isRegistrationClosed && hasNotStarted;
  }

  isUserPreferredLeague(session: Session): boolean {
    const profile = this.userProfile();
    if (!profile || !profile.leagueId || !session.leagueId) {
      return false;
    }
    return profile.leagueId === session.leagueId;
  }

  getSessionStatus(session: Session): {
    label: string;
    class: string;
    icon: string;
  } {
    const now = new Date();
    const registrationOpen = session.registrationOpenDate
      ? new Date(session.registrationOpenDate)
      : null;
    const registrationClose = session.registrationCloseDate
      ? new Date(session.registrationCloseDate)
      : null;

    // Check if full
    if (session.isFull) {
      return { label: 'Full', class: 'bg-danger', icon: 'bi-x-circle-fill' };
    }

    // Check if opening soon
    if (registrationOpen && registrationOpen > now) {
      return {
        label: 'Opening Soon',
        class: 'bg-info',
        icon: 'bi-clock-fill',
      };
    }

    // Check if closed
    if (registrationClose && registrationClose <= now) {
      return {
        label: 'Closed',
        class: 'bg-secondary',
        icon: 'bi-lock-fill',
      };
    }

    // Check if spots running low
    if (
      session.spotsLeft !== undefined &&
      session.spotsLeft <= 5 &&
      session.spotsLeft > 0
    ) {
      return {
        label: `${session.spotsLeft} Spots Left`,
        class: 'bg-warning text-dark',
        icon: 'bi-exclamation-triangle-fill',
      };
    }

    // Open for registration
    return { label: 'Open', class: 'bg-success', icon: 'bi-check-circle-fill' };
  }

  getHeaderColor(session: Session): string {
    // Use session ID to get consistent color for each session
    const index = session.id % this.headerColors.length;
    return this.headerColors[index];
  }

  ngOnInit() {
    // Load user profile if authenticated (for league highlighting)
    this.dataService.getProfile().subscribe({
      next: (data) => {
        this.userProfile.set(data);
      },
      error: (err) => {
        // User not authenticated - that's fine for public sessions page
        console.log('User not authenticated, skipping profile load');
      },
    });

    this.loadSessions();
  }

  loadSessions(): void {
    this.isLoading.set(true);

    this.dataService
      .getSessions()
      .pipe(
        tap((data) => {
          console.log('Sessions loaded:', data);
          // Filter to only show sessions that are active AND (opening soon or open for registration)
          const relevantSessions = data.filter((session) => {
            const isActive = session.isActive;
            const openingSoon = this.isRegistrationOpeningSoon(session);
            const isOpen = this.isRegistrationOpen(session);

            console.log(`Session "${session.name}":`, {
              isActive,
              openingSoon,
              isOpen,
              registrationOpenDate: session.registrationOpenDate,
            });

            return isActive && (openingSoon || isOpen);
          });
          console.log(
            'Relevant sessions (active and opening soon or open):',
            relevantSessions,
          );

          // Sort sessions: by registration open date if it exists, otherwise by start date
          const sortedSessions = relevantSessions.sort((a, b) => {
            const aDate = a.registrationOpenDate
              ? new Date(a.registrationOpenDate).getTime()
              : new Date(a.startDate).getTime();
            const bDate = b.registrationOpenDate
              ? new Date(b.registrationOpenDate).getTime()
              : new Date(b.startDate).getTime();
            return aDate - bDate; // Ascending order (earliest first)
          });

          this.sessions.set(sortedSessions);
          this.errorMessage.set(null);
          this.isLoading.set(false);
        }),
        catchError((err) => {
          this.errorMessage.set(
            err.error?.message || 'Failed to fetch sessions. Please try again.',
          );
          console.error('Error fetching sessions:', err);
          this.isLoading.set(false);
          return of([]);
        }),
      )
      .subscribe();
  }
}
