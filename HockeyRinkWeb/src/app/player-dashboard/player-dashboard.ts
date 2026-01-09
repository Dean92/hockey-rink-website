import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { AuthService } from '../auth';
import { DataService } from '../data';

interface TeamAssignment {
  sessionId: number;
  sessionName: string;
  sessionDate: string;
  leagueName: string;
  teamId: number;
  teamName: string;
  teamColor: string | null;
  isCaptain: boolean;
  sessionRecord: string | null;
  standing: string | null;
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
}

interface Teammate {
  name: string;
  position: string;
  email: string | null;
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
  showTeamModal = signal<boolean>(false);
  selectedTeam = signal<TeamDetails | null>(null);
  loadingTeamDetails = signal<boolean>(false);

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

        // Separate into current and past based on session date
        const now = new Date();
        const current: TeamAssignment[] = [];
        const past: TeamAssignment[] = [];

        teams.forEach((team) => {
          const sessionDate = new Date(team.sessionDate);
          if (sessionDate >= now) {
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

  viewTeamDetails(sessionId: number) {
    this.loadingTeamDetails.set(true);
    this.showTeamModal.set(true);
    this.selectedTeam.set(null);

    this.dataService.getMyTeam(sessionId).subscribe({
      next: (team) => {
        this.selectedTeam.set(team);
        this.loadingTeamDetails.set(false);
      },
      error: (err) => {
        console.error('Error loading team details:', err);
        this.errorMessage.set(
          err.error?.message || 'Failed to load team details'
        );
        this.loadingTeamDetails.set(false);
        this.showTeamModal.set(false);
      },
    });
  }

  closeTeamModal() {
    this.showTeamModal.set(false);
    this.selectedTeam.set(null);
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
