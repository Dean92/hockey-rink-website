import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { AuthService } from '../auth';
import { DataService } from '../data';

interface TeamAssignment {
  sessionId: number;
  sessionName: string;
  sessionDate: string;
  sessionEndDate: string;
  leagueName: string;
  teamId: number;
  teamName: string;
  teamColor: string | null;
  isCaptain: boolean;
  sessionRecord: string | null;
  standing: string | null;
  jerseyNumber?: number | null;
}

interface TeamDetails {
  teamId: number;
  teamName: string;
  teamColor: string | null;
  captainName: string;
  isCaptain: boolean;
  teammates: Teammate[];
  sessionName: string;
  sessionDate: string;
  leagueName: string | null;
  sessionRecord: string | null;
  standing: string | null;
  jerseyNumber?: number | null;
}

interface Teammate {
  name: string;
  position: string;
  email: string | null;
  jerseyNumber?: number | null;
}

@Component({
  selector: 'app-player-dashboard',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './player-dashboard.html',
  styleUrls: ['./player-dashboard.css'],
})
export class PlayerDashboard implements OnInit {
  teams = signal<TeamAssignment[]>([]);
  currentTeams = signal<TeamAssignment[]>([]);
  pastTeams = signal<TeamAssignment[]>([]);
  isLoading = signal<boolean>(true);
  errorMessage = signal<string | null>(null);
  expandedTeamId = signal<number | null>(null);
  teamDetails = signal<Map<number, TeamDetails>>(new Map());
  loadingTeamId = signal<number | null>(null);

  constructor(
    private dataService: DataService,
    private authService: AuthService,
    private router: Router
  ) {}

  ngOnInit() {
    this.loadMyTeams();
  }

  loadMyTeams() {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    this.dataService.getMyTeams().subscribe({
      next: (teams) => {
        this.teams.set(teams);

        // Separate into current and past based on session end date
        // Current: session end date is today or in the future
        // Past: session end date has passed
        const now = new Date();
        now.setHours(0, 0, 0, 0); // Compare dates only, not time
        const current: TeamAssignment[] = [];
        const past: TeamAssignment[] = [];

        teams.forEach((team) => {
          const sessionEndDate = new Date(team.sessionEndDate);
          sessionEndDate.setHours(0, 0, 0, 0); // Compare dates only, not time

          // If session end date is today or in the future, it's current
          if (sessionEndDate >= now) {
            current.push(team);
          } else {
            past.push(team);
          }
        });

        this.currentTeams.set(current);
        this.pastTeams.set(past);
        this.isLoading.set(false);
      },
      error: (err) => {
        console.error('Error loading teams:', err);
        this.errorMessage.set(
          err.error?.message || 'Failed to load your team assignments'
        );
        this.isLoading.set(false);
      },
    });
  }

  toggleRoster(sessionId: number) {
    // If clicking the same team, collapse it
    if (this.expandedTeamId() === sessionId) {
      this.expandedTeamId.set(null);
      return;
    }

    // If team details already loaded, just expand
    const details = this.teamDetails();
    if (details.has(sessionId)) {
      this.expandedTeamId.set(sessionId);
      return;
    }

    // Load team details
    this.loadingTeamId.set(sessionId);
    this.dataService.getMyTeam(sessionId).subscribe({
      next: (team) => {
        const updatedDetails = new Map(this.teamDetails());
        updatedDetails.set(sessionId, team);
        this.teamDetails.set(updatedDetails);
        this.expandedTeamId.set(sessionId);
        this.loadingTeamId.set(null);
      },
      error: (err) => {
        console.error('Error loading team details:', err);
        this.errorMessage.set(
          err.error?.message || 'Failed to load team details'
        );
        this.loadingTeamId.set(null);
      },
    });
  }

  isExpanded(sessionId: number): boolean {
    return this.expandedTeamId() === sessionId;
  }

  isLoadingRoster(sessionId: number): boolean {
    return this.loadingTeamId() === sessionId;
  }

  getTeamDetails(sessionId: number): TeamDetails | undefined {
    return this.teamDetails().get(sessionId);
  }

  formatDate(dateString: string): string {
    const date = new Date(dateString);
    return date.toLocaleDateString('en-US', {
      month: 'short',
      day: 'numeric',
      year: 'numeric',
    });
  }
}
