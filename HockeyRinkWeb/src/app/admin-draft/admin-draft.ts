import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { environment } from '../../environments/environment';
import { AuthService } from '../auth';
import { ToastService } from '../services/toast.service';
import {
  CdkDragDrop,
  DragDropModule,
  moveItemInArray,
  transferArrayItem,
} from '@angular/cdk/drag-drop';

interface DraftPlayer {
  id: number;
  name: string;
  position: string;
  rating: number | null;
  email: string;
  teamId: number | null;
  teamName: string | null;
}

interface Team {
  id: number;
  teamName: string;
  captainName: string | null;
  teamColor: string | null;
  maxPlayers: number | null;
  createdAt: string;
  players: DraftPlayer[];
}

@Component({
  selector: 'app-admin-draft',
  standalone: true,
  imports: [CommonModule, RouterLink, DragDropModule],
  templateUrl: './admin-draft.html',
  styleUrl: './admin-draft.css',
})
export class AdminDraft implements OnInit {
  sessionId = signal<number>(0);
  sessionName = signal<string>('');
  availablePlayers = signal<DraftPlayer[]>([]);
  teams = signal<Team[]>([]);
  isLoading = signal<boolean>(true);
  errorMessage = signal<string | null>(null);
  showRatings = signal<boolean>(true);

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private http: HttpClient,
    private authService: AuthService,
    private toastService: ToastService
  ) {}

  ngOnInit() {
    this.route.params.subscribe((params) => {
      const id = +params['sessionId'];
      this.sessionId.set(id);
      this.loadDraftData();
    });
  }

  private getHeaders(): HttpHeaders {
    const token = this.authService.getToken();
    return new HttpHeaders({
      Authorization: `Bearer ${token}`,
      'Content-Type': 'application/json',
    });
  }

  loadDraftData() {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    // Load players and teams in parallel
    Promise.all([
      this.http
        .get<DraftPlayer[]>(
          `${
            environment.apiUrl
          }/admin/sessions/${this.sessionId()}/draft/players`,
          { headers: this.getHeaders() }
        )
        .toPromise(),
      this.http
        .get<Team[]>(
          `${environment.apiUrl}/admin/sessions/${this.sessionId()}/teams`,
          { headers: this.getHeaders() }
        )
        .toPromise(),
    ])
      .then(([players, teams]) => {
        // Separate players into available vs assigned
        const availablePlayers: DraftPlayer[] = [];
        const teamPlayersMap = new Map<number, DraftPlayer[]>();

        players?.forEach((player) => {
          if (player.teamId) {
            if (!teamPlayersMap.has(player.teamId)) {
              teamPlayersMap.set(player.teamId, []);
            }
            teamPlayersMap.get(player.teamId)!.push(player);
          } else {
            availablePlayers.push(player);
          }
        });

        // Sort available players: Goalies first, then by rating (high to low)
        availablePlayers.sort((a, b) => {
          const posA = a.position?.toUpperCase() || '';
          const posB = b.position?.toUpperCase() || '';

          // Prioritize Goalies (G or Goalie)
          const isGoalieA = posA === 'G' || posA === 'GOALIE';
          const isGoalieB = posB === 'G' || posB === 'GOALIE';

          if (isGoalieA && !isGoalieB) return -1;
          if (!isGoalieA && isGoalieB) return 1;

          // Then sort by rating (highest first)
          const ratingA = a.rating || 0;
          const ratingB = b.rating || 0;
          return ratingB - ratingA;
        });

        this.availablePlayers.set(availablePlayers);

        // Attach players to teams
        const teamsWithPlayers =
          teams?.map((team) => ({
            ...team,
            players: teamPlayersMap.get(team.id) || [],
          })) || [];

        this.teams.set(teamsWithPlayers);
        this.isLoading.set(false);
      })
      .catch((error) => {
        console.error('Error loading draft data:', error);
        let errorMsg = 'Failed to load draft data';
        if (error?.error?.message) {
          errorMsg += `: ${error.error.message}`;
        } else if (error?.message) {
          errorMsg += `: ${error.message}`;
        } else if (error?.status) {
          errorMsg += ` (Status: ${error.status})`;
        }
        this.errorMessage.set(errorMsg);
        this.isLoading.set(false);
      });
  }

  toggleRatings() {
    this.showRatings.set(!this.showRatings());
  }

  drop(event: CdkDragDrop<DraftPlayer[]>, targetTeamId?: number) {
    if (event.previousContainer === event.container) {
      // Reordering within same container
      moveItemInArray(
        event.container.data,
        event.previousIndex,
        event.currentIndex
      );
    } else {
      // Moving between containers
      const player = event.previousContainer.data[event.previousIndex];

      if (targetTeamId !== undefined) {
        // Assigning to a team
        this.assignPlayerToTeam(player, targetTeamId);
      } else {
        // Removing from team (dropping back to available pool)
        if (player.teamId) {
          this.removePlayerFromTeam(player);
        }
      }
    }
  }

  private assignPlayerToTeam(player: DraftPlayer, teamId: number) {
    this.http
      .post(
        `${
          environment.apiUrl
        }/admin/sessions/${this.sessionId()}/teams/${teamId}/players`,
        { registrationId: player.id },
        { headers: this.getHeaders() }
      )
      .subscribe({
        next: () => {
          // Update local state
          const available = [...this.availablePlayers()];
          const playerIndex = available.findIndex((p) => p.id === player.id);
          if (playerIndex > -1) {
            available.splice(playerIndex, 1);
            this.availablePlayers.set(available);
          }

          const teams = [...this.teams()];
          const team = teams.find((t) => t.id === teamId);
          if (team) {
            player.teamId = teamId;
            player.teamName = team.teamName;
            team.players.push(player);
            this.teams.set(teams);
          }

          this.toastService.success(
            'Player Assigned',
            `${player.name} assigned to ${team?.teamName}`
          );
        },
        error: (err) => {
          console.error('Error assigning player:', err);
          this.toastService.error(
            'Assignment Failed',
            err.error?.message || 'Failed to assign player'
          );
          this.loadDraftData(); // Reload to sync state
        },
      });
  }

  removePlayerFromTeam(player: DraftPlayer) {
    if (!player.teamId) return;

    this.http
      .delete(
        `${environment.apiUrl}/admin/sessions/${this.sessionId()}/teams/${
          player.teamId
        }/players/${player.id}`,
        { headers: this.getHeaders() }
      )
      .subscribe({
        next: () => {
          // Update local state
          const teams = [...this.teams()];
          const team = teams.find((t) => t.id === player.teamId);
          if (team) {
            const playerIndex = team.players.findIndex(
              (p) => p.id === player.id
            );
            if (playerIndex > -1) {
              team.players.splice(playerIndex, 1);
              this.teams.set(teams);
            }
          }

          player.teamId = null;
          player.teamName = null;
          const available = [...this.availablePlayers()];
          available.push(player);

          // Re-sort: Goalies first, then by rating
          available.sort((a, b) => {
            const posA = a.position?.toUpperCase() || '';
            const posB = b.position?.toUpperCase() || '';

            const isGoalieA = posA === 'G' || posA === 'GOALIE';
            const isGoalieB = posB === 'G' || posB === 'GOALIE';

            if (isGoalieA && !isGoalieB) return -1;
            if (!isGoalieA && isGoalieB) return 1;

            return (b.rating || 0) - (a.rating || 0);
          });

          this.availablePlayers.set(available);

          this.toastService.success(
            'Player Removed',
            `${player.name} removed from team`
          );
        },
        error: (err) => {
          console.error('Error removing player:', err);
          this.toastService.error(
            'Removal Failed',
            err.error?.message || 'Failed to remove player'
          );
          this.loadDraftData(); // Reload to sync state
        },
      });
  }

  navigateToTeams() {
    this.router.navigate(['/admin/sessions', this.sessionId(), 'teams']);
  }

  goBack() {
    this.router.navigate(['/admin/sessions']);
  }

  getTeamDropListIds(): string[] {
    return this.teams().map((team) => `team-${team.id}`);
  }

  getOtherTeamDropListIds(currentTeamId: number): string[] {
    return this.teams()
      .filter((team) => team.id !== currentTeamId)
      .map((team) => `team-${team.id}`);
  }
}
