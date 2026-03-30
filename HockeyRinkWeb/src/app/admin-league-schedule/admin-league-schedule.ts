import { CommonModule } from '@angular/common';
import { RouterLink, ActivatedRoute } from '@angular/router';
import { Component, OnInit, signal, computed } from '@angular/core';
import {
  ReactiveFormsModule,
  FormBuilder,
  FormGroup,
  Validators,
} from '@angular/forms';
import {
  AdminService,
  GameSummary,
  UpdateGameRequest,
  Rink,
} from '../admin.service';
import { DataService } from '../data';
import { League } from '../models';

type SortField = 'gameDate' | 'rinkName' | 'status';
type SortDir = 'asc' | 'desc';

@Component({
  selector: 'app-admin-league-schedule',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './admin-league-schedule.html',
  styleUrls: ['./admin-league-schedule.css'],
})
export class AdminLeagueSchedule implements OnInit {
  leagueId = signal<number>(0);
  league = signal<League | null>(null);
  allGames = signal<GameSummary[]>([]);
  rinks = signal<Rink[]>([]);
  teams = signal<{ id: number; teamName: string }[]>([]);

  isLoading = signal(false);
  errorMessage = signal<string | null>(null);
  successMessage = signal<string | null>(null);

  // ── Filters ──────────────────────────────────────────────────────────────
  filterTeamId = signal<number | null>(null);
  filterStatus = signal<string>('');
  filterStartDate = signal<string>('');
  filterEndDate = signal<string>('');
  filterRinkId = signal<number | null>(null);

  // ── Sorting ──────────────────────────────────────────────────────────────
  sortField = signal<SortField>('gameDate');
  sortDir = signal<SortDir>('asc');

  // ── Filtered + sorted view ────────────────────────────────────────────────
  filteredGames = computed(() => {
    let games = [...this.allGames()];

    const teamId = this.filterTeamId();
    if (teamId)
      games = games.filter(
        (g) => g.homeTeamId === teamId || g.awayTeamId === teamId,
      );

    const status = this.filterStatus();
    if (status) games = games.filter((g) => g.status === status);

    const start = this.filterStartDate();
    if (start)
      games = games.filter((g) => new Date(g.gameDate) >= new Date(start));

    const end = this.filterEndDate();
    if (end)
      games = games.filter(
        (g) => new Date(g.gameDate) <= new Date(end + 'T23:59:59'),
      );

    const rinkId = this.filterRinkId();
    if (rinkId) games = games.filter((g) => g.rinkId === rinkId);

    const field = this.sortField();
    const dir = this.sortDir() === 'asc' ? 1 : -1;
    games.sort((a, b) => {
      let av: string | number = '';
      let bv: string | number = '';
      if (field === 'gameDate') {
        av = a.gameDate;
        bv = b.gameDate;
      } else if (field === 'rinkName') {
        av = a.rinkName ?? '';
        bv = b.rinkName ?? '';
      } else if (field === 'status') {
        av = a.status;
        bv = b.status;
      }
      return av < bv ? -dir : av > bv ? dir : 0;
    });

    return games;
  });

  // ── Bulk selection ────────────────────────────────────────────────────────
  selectedGameIds = signal<Set<number>>(new Set());
  allSelected = computed(() => {
    const filtered = this.filteredGames();
    const selected = this.selectedGameIds();
    return filtered.length > 0 && filtered.every((g) => selected.has(g.id));
  });

  // ── Edit modal ────────────────────────────────────────────────────────────
  showEditModal = signal(false);
  editingGame = signal<GameSummary | null>(null);
  isSavingEdit = signal(false);
  editConflict = signal<string | null>(null);
  editForm: FormGroup;

  // ── Cancel confirm ────────────────────────────────────────────────────────
  showCancelConfirm = signal(false);
  gameToCancel = signal<GameSummary | null>(null);
  isCancelling = signal(false);

  // ── Bulk cancel ───────────────────────────────────────────────────────────
  showBulkCancelConfirm = signal(false);
  isBulkCancelling = signal(false);

  constructor(
    private route: ActivatedRoute,
    private adminService: AdminService,
    private dataService: DataService,
    private fb: FormBuilder,
  ) {
    this.editForm = this.fb.group({
      gameDate: ['', Validators.required],
      rinkId: [null],
      homeTeamId: ['', Validators.required],
      awayTeamId: ['', Validators.required],
      homeScore: [null],
      awayScore: [null],
      status: ['Scheduled', Validators.required],
      location: [''],
    });
  }

  ngOnInit(): void {
    this.route.params.subscribe((params) => {
      const id = +params['id'];
      this.leagueId.set(id);
      this.loadAll(id);
    });
    this.adminService.getRinks().subscribe({ next: (r) => this.rinks.set(r) });
  }

  loadAll(leagueId: number): void {
    this.isLoading.set(true);
    this.dataService.getAllLeagues().subscribe({
      next: (leagues) =>
        this.league.set(leagues.find((l) => l.id === leagueId) ?? null),
      error: () => {},
    });
    this.adminService.getLeagueGames(leagueId).subscribe({
      next: (games) => {
        this.allGames.set(games);
        // Derive unique teams from games for the filter dropdown
        const teamMap = new Map<number, string>();
        games.forEach((g) => {
          teamMap.set(g.homeTeamId, g.homeTeamName);
          teamMap.set(g.awayTeamId, g.awayTeamName);
        });
        this.teams.set(
          [...teamMap.entries()].map(([id, teamName]) => ({ id, teamName })),
        );
        this.isLoading.set(false);
      },
      error: (err) => {
        console.error(err);
        this.errorMessage.set('Failed to load games');
        this.isLoading.set(false);
      },
    });
  }

  // ── Sorting ───────────────────────────────────────────────────────────────

  sort(field: SortField): void {
    if (this.sortField() === field) {
      this.sortDir.set(this.sortDir() === 'asc' ? 'desc' : 'asc');
    } else {
      this.sortField.set(field);
      this.sortDir.set('asc');
    }
  }

  sortIcon(field: SortField): string {
    if (this.sortField() !== field) return 'bi-arrow-down-up text-muted';
    return this.sortDir() === 'asc' ? 'bi-sort-up' : 'bi-sort-down';
  }

  // ── Selection ─────────────────────────────────────────────────────────────

  toggleSelectAll(): void {
    const sel = new Set(this.selectedGameIds());
    if (this.allSelected()) {
      this.filteredGames().forEach((g) => sel.delete(g.id));
    } else {
      this.filteredGames().forEach((g) => sel.add(g.id));
    }
    this.selectedGameIds.set(sel);
  }

  toggleSelect(id: number): void {
    const sel = new Set(this.selectedGameIds());
    sel.has(id) ? sel.delete(id) : sel.add(id);
    this.selectedGameIds.set(sel);
  }

  isSelected(id: number): boolean {
    return this.selectedGameIds().has(id);
  }

  // ── Edit game ─────────────────────────────────────────────────────────────

  openEdit(game: GameSummary): void {
    this.editingGame.set(game);
    this.editConflict.set(null);
    this.editForm.patchValue({
      gameDate: this.toDateTimeLocal(game.gameDate),
      rinkId: game.rinkId ?? null,
      homeTeamId: game.homeTeamId,
      awayTeamId: game.awayTeamId,
      homeScore: game.homeScore ?? null,
      awayScore: game.awayScore ?? null,
      status: game.status,
      location: game.location ?? '',
    });
    this.showEditModal.set(true);
  }

  closeEdit(): void {
    this.showEditModal.set(false);
    this.editingGame.set(null);
    this.editConflict.set(null);
  }

  saveEdit(): void {
    if (this.editForm.invalid) {
      this.editForm.markAllAsTouched();
      return;
    }
    const game = this.editingGame();
    if (!game) return;

    const v = this.editForm.value;
    const request: UpdateGameRequest = {
      gameDate: new Date(v.gameDate).toISOString(),
      rinkId: v.rinkId || undefined,
      homeTeamId: +v.homeTeamId,
      awayTeamId: +v.awayTeamId,
      homeScore: v.homeScore ?? undefined,
      awayScore: v.awayScore ?? undefined,
      status: v.status,
      location: v.location || undefined,
      excludeGameId: game.id,
    };

    this.isSavingEdit.set(true);
    this.editConflict.set(null);

    this.adminService.updateGame(game.id, request).subscribe({
      next: (updated) => {
        const games = this.allGames().map((g) =>
          g.id === updated.id ? updated : g,
        );
        this.allGames.set(games);
        this.successMessage.set('Game updated successfully');
        this.isSavingEdit.set(false);
        this.closeEdit();
        setTimeout(() => this.successMessage.set(null), 3000);
      },
      error: (err) => {
        if (err.status === 409) {
          this.editConflict.set(
            err.error?.message ?? 'Scheduling conflict detected',
          );
        } else {
          this.errorMessage.set(err.error?.message ?? 'Failed to update game');
        }
        this.isSavingEdit.set(false);
      },
    });
  }

  // ── Cancel game ───────────────────────────────────────────────────────────

  confirmCancel(game: GameSummary): void {
    this.gameToCancel.set(game);
    this.showCancelConfirm.set(true);
  }

  cancelCancelDialog(): void {
    this.showCancelConfirm.set(false);
    this.gameToCancel.set(null);
  }

  executeCancel(): void {
    const game = this.gameToCancel();
    if (!game) return;
    this.isCancelling.set(true);
    this.adminService.cancelGame(game.id).subscribe({
      next: () => {
        this.allGames.update((games) =>
          games.map((g) =>
            g.id === game.id ? { ...g, status: 'Cancelled' } : g,
          ),
        );
        this.successMessage.set('Game cancelled');
        this.isCancelling.set(false);
        this.cancelCancelDialog();
        setTimeout(() => this.successMessage.set(null), 3000);
      },
      error: (err) => {
        this.errorMessage.set(err.error?.message ?? 'Failed to cancel game');
        this.isCancelling.set(false);
        this.cancelCancelDialog();
      },
    });
  }

  // ── Bulk cancel ───────────────────────────────────────────────────────────

  openBulkCancel(): void {
    this.showBulkCancelConfirm.set(true);
  }
  closeBulkCancel(): void {
    this.showBulkCancelConfirm.set(false);
  }

  executeBulkCancel(): void {
    const ids = [...this.selectedGameIds()];
    const scheduledIds = ids.filter((id) => {
      const g = this.allGames().find((g) => g.id === id);
      return g && g.status !== 'Cancelled';
    });

    if (scheduledIds.length === 0) {
      this.closeBulkCancel();
      return;
    }

    this.isBulkCancelling.set(true);
    let done = 0;
    scheduledIds.forEach((id) => {
      this.adminService.cancelGame(id).subscribe({
        next: () => {
          this.allGames.update((games) =>
            games.map((g) => (g.id === id ? { ...g, status: 'Cancelled' } : g)),
          );
          done++;
          if (done === scheduledIds.length) {
            this.successMessage.set(`${done} game(s) cancelled`);
            this.selectedGameIds.set(new Set());
            this.isBulkCancelling.set(false);
            this.closeBulkCancel();
            setTimeout(() => this.successMessage.set(null), 3000);
          }
        },
        error: () => {
          done++;
        },
      });
    });
  }

  // ── Print ─────────────────────────────────────────────────────────────────

  print(): void {
    window.print();
  }

  // ── Helpers ───────────────────────────────────────────────────────────────

  formatDateTime(dateStr: string): string {
    return new Date(dateStr).toLocaleString('en-US', {
      dateStyle: 'medium',
      timeStyle: 'short',
    });
  }

  toDateTimeLocal(dateStr: string): string {
    const d = new Date(dateStr);
    const y = d.getFullYear();
    const mo = (d.getMonth() + 1).toString().padStart(2, '0');
    const day = d.getDate().toString().padStart(2, '0');
    const h = d.getHours().toString().padStart(2, '0');
    const m = d.getMinutes().toString().padStart(2, '0');
    return `${y}-${mo}-${day}T${h}:${m}`;
  }

  statusBadgeClass(status: string): string {
    switch (status) {
      case 'Scheduled':
        return 'bg-primary';
      case 'InProgress':
        return 'bg-warning text-dark';
      case 'Completed':
        return 'bg-success';
      case 'Cancelled':
        return 'bg-danger';
      default:
        return 'bg-secondary';
    }
  }

  clearFilters(): void {
    this.filterTeamId.set(null);
    this.filterStatus.set('');
    this.filterStartDate.set('');
    this.filterEndDate.set('');
    this.filterRinkId.set(null);
  }
}
