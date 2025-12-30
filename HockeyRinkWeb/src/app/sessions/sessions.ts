import { Component, OnInit, signal } from '@angular/core';
import { DataService } from '../data';
import { CommonModule, DatePipe } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { tap, catchError, of } from 'rxjs';
import { Session } from '../models';

@Component({
  selector: 'app-sessions',
  standalone: true,
  imports: [CommonModule, DatePipe, ReactiveFormsModule, RouterLink],
  templateUrl: './sessions.html',
  styleUrls: ['./sessions.css'],
})
export class Sessions implements OnInit {
  leagues = signal<any[]>([]);
  sessions = signal<Session[]>([]);
  userProfile = signal<any>(null);
  errorMessage = signal<string | null>(null);
  isLoading = signal<boolean>(true);
  filterForm: FormGroup;
  currentDate = new Date();

  constructor(private dataService: DataService, private fb: FormBuilder) {
    this.filterForm = this.fb.group({
      league: [''],
      date: [''],
    });
  }

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

    this.dataService
      .getLeagues()
      .pipe(
        tap((data) => {
          console.log('Leagues loaded:', data);
          this.leagues.set(data);
        }),
        catchError((err) => {
          console.error('Error fetching leagues:', err);
          this.errorMessage.set(
            err.error?.message || 'Failed to fetch leagues. Please try again.'
          );
          return of([]);
        })
      )
      .subscribe();

    this.applyFilters();

    this.filterForm.valueChanges
      .pipe(tap(() => this.applyFilters()))
      .subscribe();
  }

  applyFilters(): void {
    this.isLoading.set(true);
    const { league, date } = this.filterForm.value;
    const selectedLeagueId = league ? Number(league) : undefined;
    const selectedDate = date ? new Date(date) : undefined;

    console.log('Applying filters:', { selectedLeagueId, selectedDate });

    this.dataService
      .getSessions(selectedLeagueId, selectedDate)
      .pipe(
        tap((data) => {
          console.log('Sessions loaded:', data);
          this.sessions.set(data);
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
