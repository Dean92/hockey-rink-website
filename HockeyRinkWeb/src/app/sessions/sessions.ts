import { Component, OnInit, signal } from '@angular/core';
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
  currentDate = new Date();

  constructor(private dataService: DataService) {}

  isEarlyBirdActive(session: Session): boolean {
    if (!session.earlyBirdPrice || !session.earlyBirdEndDate) {
      return false;
    }
    const earlyBirdEnd = new Date(session.earlyBirdEndDate);
    const registrationOpen = session.registrationOpenDate
      ? new Date(session.registrationOpenDate)
      : null;

    const isWithinEarlyBirdPeriod = earlyBirdEnd > this.currentDate;
    const isRegistrationOpen =
      !registrationOpen || registrationOpen <= this.currentDate;

    return isWithinEarlyBirdPeriod && isRegistrationOpen;
  }

  isRegistrationOpen(session: Session): boolean {
    const registrationOpen = session.registrationOpenDate
      ? new Date(session.registrationOpenDate)
      : null;
    const registrationClose = session.registrationCloseDate
      ? new Date(session.registrationCloseDate)
      : null;

    const hasOpened = !registrationOpen || registrationOpen <= this.currentDate;
    const hasNotClosed =
      !registrationClose || registrationClose > this.currentDate;

    return hasOpened && hasNotClosed;
  }

  isRegistrationOpeningSoon(session: Session): boolean {
    const registrationOpen = session.registrationOpenDate
      ? new Date(session.registrationOpenDate)
      : null;

    // Check if registration has a future open date
    return !!registrationOpen && registrationOpen > this.currentDate;
  }

  isSessionStartingSoon(session: Session): boolean {
    const registrationClose = session.registrationCloseDate
      ? new Date(session.registrationCloseDate)
      : null;
    const sessionStart = new Date(session.startDate);

    // Check if registration is closed but session hasn't started yet
    const isRegistrationClosed =
      !!registrationClose && registrationClose <= this.currentDate;
    const hasNotStarted = sessionStart > this.currentDate;

    return isRegistrationClosed && hasNotStarted;
  }

  isUserPreferredLeague(session: Session): boolean {
    const profile = this.userProfile();
    if (!profile || !profile.leagueId || !session.leagueId) {
      return false;
    }
    return profile.leagueId === session.leagueId;
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
          // Filter to only show sessions that are opening soon or open for registration
          const relevantSessions = data.filter((session) => {
            return (
              this.isRegistrationOpeningSoon(session) ||
              this.isRegistrationOpen(session)
            );
          });
          console.log(
            'Relevant sessions (opening soon or open):',
            relevantSessions
          );
          this.sessions.set(relevantSessions);
          this.errorMessage.set(null);
          this.isLoading.set(false);
        }),
        catchError((err) => {
          this.errorMessage.set(
            err.error?.message || 'Failed to fetch sessions. Please try again.'
          );
          console.error('Error fetching sessions:', err);
          this.isLoading.set(false);
          return of([]);
        })
      )
      .subscribe();
  }
}
