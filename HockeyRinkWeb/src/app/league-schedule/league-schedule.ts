import { CommonModule } from '@angular/common';
import { RouterLink, ActivatedRoute } from '@angular/router';
import { Component, OnInit, signal, computed } from '@angular/core';
import { DataService } from '../data';
import { League } from '../models';

@Component({
  selector: 'app-league-schedule',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './league-schedule.html',
  styleUrls: ['./league-schedule.css'],
})
export class LeagueSchedule implements OnInit {
  leagueId = signal<number>(0);
  league = signal<League | null>(null);
  allGames = signal<any[]>([]);
  isLoading = signal(false);
  errorMessage = signal<string | null>(null);

  // ── Filters ──────────────────────────────────────────────────────────────
  filterTeamId = signal<number | null>(null);
  filterStartDate = signal<string>('');
  filterEndDate = signal<string>('');

  // ── Teams derived from games ──────────────────────────────────────────────
  teams = computed(() => {
    const teamMap = new Map<number, string>();
    this.allGames().forEach((g) => {
      teamMap.set(g.homeTeamId, g.homeTeamName);
      teamMap.set(g.awayTeamId, g.awayTeamName);
    });
    return [...teamMap.entries()].map(([id, teamName]) => ({ id, teamName }));
  });

  filteredGames = computed(() => {
    let games = [...this.allGames()];

    const teamId = this.filterTeamId();
    if (teamId)
      games = games.filter(
        (g) => g.homeTeamId === teamId || g.awayTeamId === teamId,
      );

    const start = this.filterStartDate();
    if (start)
      games = games.filter((g) => new Date(g.gameDate) >= new Date(start));

    const end = this.filterEndDate();
    if (end)
      games = games.filter(
        (g) => new Date(g.gameDate) <= new Date(end + 'T23:59:59'),
      );

    return games.sort(
      (a, b) => new Date(a.gameDate).getTime() - new Date(b.gameDate).getTime(),
    );
  });

  constructor(
    private route: ActivatedRoute,
    private dataService: DataService,
  ) {}

  ngOnInit(): void {
    this.route.params.subscribe((params) => {
      const id = +params['id'];
      this.leagueId.set(id);
      this.loadData(id);
    });
  }

  loadData(leagueId: number): void {
    this.isLoading.set(true);

    this.dataService.getLeagues().subscribe({
      next: (leagues) =>
        this.league.set(leagues.find((l) => l.id === leagueId) ?? null),
      error: () => {},
    });

    this.dataService.getPublicLeagueGames(leagueId).subscribe({
      next: (games) => {
        this.allGames.set(games);
        this.isLoading.set(false);
      },
      error: (err) => {
        console.error(err);
        this.errorMessage.set('Failed to load schedule');
        this.isLoading.set(false);
      },
    });
  }

  clearFilters(): void {
    this.filterTeamId.set(null);
    this.filterStartDate.set('');
    this.filterEndDate.set('');
  }

  formatDateTime(dateStr: string): string {
    return new Date(dateStr).toLocaleString('en-US', {
      dateStyle: 'medium',
      timeStyle: 'short',
    });
  }

  statusBadgeClass(status: string): string {
    switch (status) {
      case 'Scheduled':
        return 'bg-primary';
      case 'InProgress':
        return 'bg-warning text-dark';
      case 'Completed':
        return 'bg-success';
      default:
        return 'bg-secondary';
    }
  }
}
