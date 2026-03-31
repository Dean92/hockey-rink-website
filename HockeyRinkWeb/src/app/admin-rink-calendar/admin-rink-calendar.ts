import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { Component, OnInit, signal, computed } from '@angular/core';
import {
  ReactiveFormsModule,
  FormBuilder,
  FormGroup,
  Validators,
} from '@angular/forms';
import {
  AdminService,
  Rink,
  CalendarSlot,
  DayBookingsResponse,
  CreateBlockoutRequest,
  ConflictCheckResponse,
  GenerateScheduleRequest,
  GenerateScheduleResponse,
  GeneratePlayoffRequest,
  ProposedGame,
  ConfirmScheduleRequest,
} from '../admin.service';
import { DataService } from '../data';
import { League } from '../models';

interface CalendarDay {
  date: Date;
  isCurrentMonth: boolean;
  isToday: boolean;
  isSelected: boolean;
  bookingCount: number;
}

interface TimeRow {
  hour: number;
  label: string;
  slots: CalendarSlot[];
}

@Component({
  selector: 'app-admin-rink-calendar',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './admin-rink-calendar.html',
  styleUrls: ['./admin-rink-calendar.css'],
})
export class AdminRinkCalendar implements OnInit {
  // ── State ──────────────────────────────────────────────────────────────────
  rinks = signal<Rink[]>([]);
  selectedRinkId = signal<number | null>(null);
  selectedDate = signal<Date>(new Date());
  viewMonth = signal<Date>(new Date());
  calendarDays = signal<CalendarDay[]>([]);
  monthCounts = signal<Record<number, number>>({});
  daySlots = signal<CalendarSlot[]>([]);
  isLoadingSlots = signal(false);
  isLoadingMonth = signal(false);
  errorMessage = signal<string | null>(null);
  successMessage = signal<string | null>(null);

  // ── Slot detail popover ────────────────────────────────────────────────────
  selectedSlot = signal<CalendarSlot | null>(null);
  showSlotDetail = signal(false);

  // ── Blockout form ──────────────────────────────────────────────────────────
  showBlockoutForm = signal(false);
  isSubmittingBlockout = signal(false);
  blockoutForm: FormGroup;

  // ── Event modal (unified: create, edit-event, edit-game, view-session) ─────
  showEventModal = signal(false);
  eventModalMode = signal<'create' | 'edit-event' | 'edit-game' | 'view-session'>('create');
  editingSlot = signal<CalendarSlot | null>(null);
  isSavingEvent = signal(false);
  isDeletingEvent = signal(false);
  isCheckingConflict = signal(false);
  conflictResult = signal<ConflictCheckResponse | null>(null);
  gameEditForm: FormGroup;

  readonly eventTypes = [
    { value: 'Blockout', label: 'Blockout' },
    { value: 'Ice Rental', label: 'Ice Rental' },
    { value: 'Private Party', label: 'Private Party' },
    { value: 'League Game', label: 'League Game' },
    { value: 'Youth Game', label: 'Youth Game' },
    { value: 'Public Skating', label: 'Public Skating' },
    { value: 'Other', label: 'Other' },
  ];

  // ── Scheduler panel ───────────────────────────────────────────────────────
  showSchedulerPanel = signal(false);
  leagues = signal<League[]>([]);
  schedulerSessions = signal<any[]>([]);
  schedulePreview = signal<GenerateScheduleResponse | null>(null);
  isGenerating = signal(false);
  isConfirming = signal(false);
  removedPreviewIndices = signal<Set<number>>(new Set());
  schedulerForm: FormGroup;

  // ── Playoff panel ─────────────────────────────────────────────────────────
  playoffPreview = signal<GenerateScheduleResponse | null>(null);
  isGeneratingPlayoffs = signal(false);
  isConfirmingPlayoffs = signal(false);
  removedPlayoffIndices = signal<Set<number>>(new Set());
  playoffForm: FormGroup;

  previewGames = computed(() => {
    const preview = this.schedulePreview();
    if (!preview) return [];
    const removed = this.removedPreviewIndices();
    return preview.proposedGames.filter((_, i) => !removed.has(i));
  });

  playoffGames = computed(() => {
    const preview = this.playoffPreview();
    if (!preview) return [];
    const removed = this.removedPlayoffIndices();
    return preview.proposedGames.filter((_, i) => !removed.has(i));
  });

  // ── Timeline hours: 5am (5) to midnight (24 = next day 0) ─────────────────
  readonly timelineHours: number[] = Array.from(
    { length: 19 },
    (_, i) => i + 5,
  );

  // ── Computed timeline rows ─────────────────────────────────────────────────
  timelineRows = computed<TimeRow[]>(() => {
    const slots = this.daySlots();
    return this.timelineHours.map((hour) => ({
      hour,
      label: this.formatHourLabel(hour),
      slots: slots.filter((s) => {
        const rowStart = new Date(s.startDateTime);
        rowStart.setHours(hour % 24, 0, 0, 0);
        const rowEnd = new Date(rowStart);
        rowEnd.setHours(hour % 24, 59, 59, 999);

        const slotStart = new Date(s.startDateTime);
        const slotEnd = new Date(s.endDateTime);

        // Slot overlaps this hour row if it starts before the row ends AND ends after the row starts
        return slotStart <= rowEnd && slotEnd > rowStart;
      }),
    }));
  });

  selectedRink = computed(
    () => this.rinks().find((r) => r.id === this.selectedRinkId()) ?? null,
  );

  constructor(
    private adminService: AdminService,
    private dataService: DataService,
    private fb: FormBuilder,
  ) {
    this.blockoutForm = this.fb.group({
      eventType: ['Blockout', Validators.required],
      startDateTime: ['', Validators.required],
      endDateTime: ['', Validators.required],
      reason: [''],
    });

    this.gameEditForm = this.fb.group({
      gameDate: ['', Validators.required],
      gameTime: ['', Validators.required],
    });

    const today = new Date();
    const thirtyDays = new Date(today);
    thirtyDays.setDate(thirtyDays.getDate() + 30);

    this.schedulerForm = this.fb.group({
      leagueId: ['', Validators.required],
      sessionId: ['', Validators.required],
      startDate: [this.toISODateStatic(today), Validators.required],
      endDate: [this.toISODateStatic(thirtyDays), Validators.required],
      daysOfWeek: this.fb.group({
        sun: [false], mon: [false], tue: [false], wed: [false],
        thu: [false], fri: [false], sat: [false],
      }),
      dailyStartTime: ['18:00', Validators.required],
      dailyEndTime: ['22:00', Validators.required],
      gameLengthMinutes: [90, [Validators.required, Validators.min(30), Validators.max(240)]],
      bufferMinutes: [10, [Validators.required, Validators.min(0), Validators.max(60)]],
      gamesPerNight: [2, [Validators.required, Validators.min(1)]],
      totalGamesPerTeam: [null, [Validators.min(1), Validators.max(200)]],
      excludeUsHolidays: [true],
      excludeDates: [''],
    });

    const playoffStart = new Date(today);
    playoffStart.setDate(playoffStart.getDate() + 60);
    const playoffEnd = new Date(playoffStart);
    playoffEnd.setDate(playoffEnd.getDate() + 30);

    this.playoffForm = this.fb.group({
      startDate: [this.toISODateStatic(playoffStart), Validators.required],
      endDate: [this.toISODateStatic(playoffEnd), Validators.required],
      daysOfWeek: this.fb.group({
        sun: [false], mon: [false], tue: [false], wed: [false],
        thu: [false], fri: [false], sat: [false],
      }),
      dailyStartTime: ['18:00', Validators.required],
      dailyEndTime: ['22:00', Validators.required],
      gameLengthMinutes: [90, [Validators.required, Validators.min(30), Validators.max(240)]],
      bufferMinutes: [10, [Validators.required, Validators.min(0), Validators.max(60)]],
      gamesPerNight: [2, [Validators.required, Validators.min(1)]],
      excludeUsHolidays: [true],
      excludeDates: [''],
    });
  }

  ngOnInit(): void {
    this.loadRinks();
    this.dataService.getAllLeagues().subscribe({ next: l => this.leagues.set(l), error: () => {} });
  }

  // ── Data loading ───────────────────────────────────────────────────────────

  loadRinks(): void {
    this.adminService.getRinks().subscribe({
      next: (rinks) => {
        this.rinks.set(rinks);
        if (rinks.length > 0) {
          this.selectedRinkId.set(rinks[0].id);
          this.loadMonthData();
          this.loadDayBookings();
        }
      },
      error: (err) => {
        console.error('Error loading rinks:', err);
        this.errorMessage.set('Failed to load rinks');
      },
    });
  }

  loadMonthData(): void {
    const rinkId = this.selectedRinkId();
    const month = this.viewMonth();
    if (!rinkId) return;

    this.isLoadingMonth.set(true);
    this.adminService
      .getRinkMonthCounts(rinkId, month.getFullYear(), month.getMonth() + 1)
      .subscribe({
        next: (res) => {
          this.monthCounts.set(res.dayCounts);
          this.buildCalendar();
          this.isLoadingMonth.set(false);
        },
        error: (err) => {
          console.error('Error loading month counts:', err);
          this.isLoadingMonth.set(false);
          this.buildCalendar();
        },
      });
  }

  loadDayBookings(): void {
    const rinkId = this.selectedRinkId();
    const date = this.selectedDate();
    if (!rinkId) return;

    this.isLoadingSlots.set(true);
    this.errorMessage.set(null);
    const dateStr = this.toISODate(date);

    this.adminService.getRinkDayBookings(rinkId, dateStr).subscribe({
      next: (res: DayBookingsResponse) => {
        this.daySlots.set(res.slots);
        this.isLoadingSlots.set(false);
      },
      error: (err) => {
        console.error('Error loading day bookings:', err);
        this.errorMessage.set('Failed to load bookings for this day');
        this.daySlots.set([]);
        this.isLoadingSlots.set(false);
      },
    });
  }

  // ── Calendar navigation ────────────────────────────────────────────────────

  buildCalendar(): void {
    const month = this.viewMonth();
    const counts = this.monthCounts();
    const today = new Date();
    const selected = this.selectedDate();

    const firstDay = new Date(month.getFullYear(), month.getMonth(), 1);
    const lastDay = new Date(month.getFullYear(), month.getMonth() + 1, 0);

    const days: CalendarDay[] = [];

    // Fill leading days from previous month
    const startDow = firstDay.getDay();
    for (let i = startDow - 1; i >= 0; i--) {
      const d = new Date(firstDay);
      d.setDate(d.getDate() - i - 1);
      days.push(this.makeDay(d, false, today, selected, counts));
    }

    // Current month days
    for (let d = 1; d <= lastDay.getDate(); d++) {
      const date = new Date(month.getFullYear(), month.getMonth(), d);
      days.push(this.makeDay(date, true, today, selected, counts));
    }

    // Fill trailing days
    const remaining = 42 - days.length;
    for (let i = 1; i <= remaining; i++) {
      const d = new Date(lastDay);
      d.setDate(d.getDate() + i);
      days.push(this.makeDay(d, false, today, selected, counts));
    }

    this.calendarDays.set(days);
  }

  private makeDay(
    date: Date,
    isCurrentMonth: boolean,
    today: Date,
    selected: Date,
    counts: Record<number, number>,
  ): CalendarDay {
    return {
      date,
      isCurrentMonth,
      isToday: this.isSameDay(date, today),
      isSelected: this.isSameDay(date, selected),
      bookingCount: isCurrentMonth ? (counts[date.getDate()] ?? 0) : 0,
    };
  }

  prevMonth(): void {
    const m = this.viewMonth();
    this.viewMonth.set(new Date(m.getFullYear(), m.getMonth() - 1, 1));
    this.loadMonthData();
  }

  nextMonth(): void {
    const m = this.viewMonth();
    this.viewMonth.set(new Date(m.getFullYear(), m.getMonth() + 1, 1));
    this.loadMonthData();
  }

  selectDay(day: CalendarDay): void {
    this.selectedDate.set(day.date);
    this.buildCalendar(); // refresh selection highlight
    this.loadDayBookings();
  }

  onRinkChange(event: Event): void {
    const value = (event.target as HTMLSelectElement).value;
    this.selectedRinkId.set(value ? +value : null);
    this.loadMonthData();
    this.loadDayBookings();
  }

  // ── Slot interactions ──────────────────────────────────────────────────────

  showDetail(slot: CalendarSlot): void {
    this.selectedSlot.set(slot);
    this.showSlotDetail.set(true);
  }

  closeDetail(): void {
    this.showSlotDetail.set(false);
    this.selectedSlot.set(null);
  }

  // ── Event modal ────────────────────────────────────────────────────────────

  openCreateEventModal(hour?: number): void {
    const date = this.selectedDate();
    const dateStr = this.toISODate(date);
    const startHour = hour ?? 9;
    const endHour = Math.min(startHour + 2, 23);

    this.blockoutForm.reset({
      eventType: 'Blockout',
      startDateTime: `${dateStr}T${startHour.toString().padStart(2, '0')}:00`,
      endDateTime: `${dateStr}T${endHour.toString().padStart(2, '0')}:00`,
      reason: '',
    });
    this.editingSlot.set(null);
    this.conflictResult.set(null);
    this.eventModalMode.set('create');
    this.showEventModal.set(true);
    this.errorMessage.set(null);
  }

  onRowDoubleClick(hour: number): void {
    this.openCreateEventModal(hour);
  }

  onSlotDoubleClick(slot: CalendarSlot, event: MouseEvent): void {
    event.stopPropagation();
    this.closeDetail();
    this.openSlotModal(slot);
  }

  openSlotModal(slot: CalendarSlot): void {
    this.editingSlot.set(slot);
    this.conflictResult.set(null);
    this.errorMessage.set(null);

    if (slot.type === 'blockout') {
      const startDt = new Date(slot.startDateTime);
      const endDt = new Date(slot.endDateTime);
      this.blockoutForm.reset({
        eventType: slot.eventType ?? 'Blockout',
        startDateTime: this.toLocalDateTimeInput(startDt),
        endDateTime: this.toLocalDateTimeInput(endDt),
        reason: slot.reason ?? '',
      });
      this.eventModalMode.set('edit-event');
      this.showEventModal.set(true);
    } else if (slot.type === 'game') {
      const gameDt = new Date(slot.startDateTime);
      this.gameEditForm.reset({
        gameDate: this.toISODate(gameDt),
        gameTime: `${gameDt.getHours().toString().padStart(2, '0')}:${gameDt.getMinutes().toString().padStart(2, '0')}`,
      });
      this.eventModalMode.set('edit-game');
      this.showEventModal.set(true);
    } else {
      this.eventModalMode.set('view-session');
      this.showEventModal.set(true);
    }
  }

  closeEventModal(): void {
    this.showEventModal.set(false);
    this.editingSlot.set(null);
    this.conflictResult.set(null);
    this.errorMessage.set(null);
    this.blockoutForm.reset();
    this.gameEditForm.reset();
  }

  saveEvent(): void {
    if (this.eventModalMode() === 'edit-game') {
      this.saveGameEdit();
    } else {
      this.saveBlockoutEvent();
    }
  }

  private saveBlockoutEvent(): void {
    if (this.blockoutForm.invalid) {
      this.blockoutForm.markAllAsTouched();
      return;
    }

    const rinkId = this.selectedRinkId();
    if (!rinkId) return;

    const { eventType, startDateTime, endDateTime, reason } = this.blockoutForm.value;
    if (new Date(endDateTime) <= new Date(startDateTime)) {
      this.errorMessage.set('End time must be after start time');
      return;
    }

    const request: CreateBlockoutRequest = {
      eventType: eventType ?? 'Blockout',
      startDateTime,
      endDateTime,
      reason: reason || undefined,
    };

    this.isSavingEvent.set(true);
    const slot = this.editingSlot();
    const obs = slot
      ? this.adminService.updateBlockout(rinkId, slot.id, request)
      : this.adminService.createBlockout(rinkId, request);

    obs.subscribe({
      next: () => {
        this.successMessage.set(slot ? 'Event updated successfully' : 'Event created successfully');
        this.isSavingEvent.set(false);
        this.closeEventModal();
        this.loadMonthData();
        this.loadDayBookings();
        setTimeout(() => this.successMessage.set(null), 3000);
      },
      error: (err) => {
        console.error('Error saving event:', err);
        this.errorMessage.set(err.error?.message ?? 'Failed to save event');
        this.isSavingEvent.set(false);
      },
    });
  }

  saveGameEdit(): void {
    if (this.gameEditForm.invalid) {
      this.gameEditForm.markAllAsTouched();
      return;
    }

    const slot = this.editingSlot();
    if (!slot || !slot.homeTeamId || !slot.awayTeamId) return;

    const { gameDate, gameTime } = this.gameEditForm.value;
    const gameDateStr = `${gameDate}T${gameTime}:00`;

    this.isSavingEvent.set(true);
    this.conflictResult.set(null);

    this.adminService.checkConflict({
      rinkId: slot.rinkId,
      startDateTime: gameDateStr,
      endDateTime: new Date(new Date(gameDateStr).getTime() + 90 * 60000).toISOString(),
      excludeGameId: slot.id,
    }).subscribe({
      next: (result) => {
        if (result.hasConflict) {
          this.conflictResult.set(result);
          this.isSavingEvent.set(false);
          return;
        }

        this.adminService.updateGame(slot.id, {
          gameDate: gameDateStr,
          rinkId: slot.rinkId,
          homeTeamId: slot.homeTeamId!,
          awayTeamId: slot.awayTeamId!,
          excludeGameId: slot.id,
        }).subscribe({
          next: () => {
            this.successMessage.set('Game rescheduled successfully');
            this.isSavingEvent.set(false);
            this.closeEventModal();
            this.loadMonthData();
            this.loadDayBookings();
            setTimeout(() => this.successMessage.set(null), 3000);
          },
          error: (err) => {
            this.errorMessage.set(err.error?.message ?? 'Failed to reschedule game');
            this.isSavingEvent.set(false);
          },
        });
      },
      error: (err) => {
        this.errorMessage.set(err.error?.message ?? 'Failed to check conflicts');
        this.isSavingEvent.set(false);
      },
    });
  }

  deleteEvent(): void {
    const slot = this.editingSlot();
    if (!slot) return;

    const rinkId = this.selectedRinkId();
    const label = slot.type === 'game' ? 'Cancel this game?' : `Remove "${slot.title}"?`;
    if (!confirm(label)) return;

    this.isDeletingEvent.set(true);

    const obs = slot.type === 'blockout'
      ? this.adminService.deleteBlockout(rinkId!, slot.id)
      : this.adminService.cancelGame(slot.id);

    obs.subscribe({
      next: () => {
        this.successMessage.set(slot.type === 'game' ? 'Game cancelled' : 'Event removed');
        this.isDeletingEvent.set(false);
        this.closeEventModal();
        this.loadMonthData();
        this.loadDayBookings();
        setTimeout(() => this.successMessage.set(null), 3000);
      },
      error: (err) => {
        this.errorMessage.set(err.error?.message ?? 'Failed to delete event');
        this.isDeletingEvent.set(false);
      },
    });
  }

  // ── Blockout form (kept for backward compat with popover's Remove button) ──

  openBlockoutForm(): void {
    this.openCreateEventModal();
  }

  closeBlockoutForm(): void {
    this.closeEventModal();
  }

  submitBlockout(): void {
    this.saveEvent();
  }

  // ── Scheduler panel ───────────────────────────────────────────────────────

  openSchedulerPanel(): void {
    this.schedulePreview.set(null);
    this.removedPreviewIndices.set(new Set());
    this.showSchedulerPanel.set(true);
  }

  closeSchedulerPanel(): void {
    this.showSchedulerPanel.set(false);
    this.schedulePreview.set(null);
  }

  onSchedulerLeagueChange(event: Event): void {
    const leagueId = +(event.target as HTMLSelectElement).value;
    if (!leagueId) { this.schedulerSessions.set([]); return; }
    this.dataService.getSessions().subscribe({
      next: sessions => this.schedulerSessions.set(sessions.filter((s: any) => s.leagueId === leagueId)),
      error: () => {}
    });
  }

  onSchedulerSessionChange(event: Event): void {
    const sessionId = +(event.target as HTMLSelectElement).value;
    const session = this.schedulerSessions().find((s: any) => s.id === sessionId);
    if (session?.regularSeasonGames) {
      this.schedulerForm.patchValue({ totalGamesPerTeam: session.regularSeasonGames });
    }
  }

  generatePreview(): void {
    if (this.schedulerForm.invalid) { this.schedulerForm.markAllAsTouched(); return; }
    const v = this.schedulerForm.value;

    const dowGroup = v.daysOfWeek;
    const dowMap: Record<string, number> = { sun: 0, mon: 1, tue: 2, wed: 3, thu: 4, fri: 5, sat: 6 };
    const daysOfWeek = Object.entries(dowMap).filter(([k]) => dowGroup[k]).map(([, n]) => n);
    if (daysOfWeek.length === 0) {
      this.errorMessage.set('Select at least one day of the week');
      return;
    }

    const excludeDatesArr = (v.excludeDates as string)
      .split(/[\n,]+/).map((s: string) => s.trim()).filter((s: string) => s.length > 0);

    const rinkId = this.selectedRinkId();
    if (!rinkId) { this.errorMessage.set('Select a rink first'); return; }

    const request: GenerateScheduleRequest = {
      leagueId: +v.leagueId,
      sessionId: +v.sessionId,
      rinkId,
      startDate: v.startDate,
      endDate: v.endDate,
      daysOfWeek,
      dailyStartTime: v.dailyStartTime + ':00',
      dailyEndTime: v.dailyEndTime + ':00',
      gameLengthMinutes: +v.gameLengthMinutes,
      bufferMinutes: +v.bufferMinutes,
      gamesPerNight: +v.gamesPerNight,
      gamesPerMatchup: 1, // fallback; overridden by totalGamesPerTeam
      totalGamesPerTeam: v.totalGamesPerTeam ? +v.totalGamesPerTeam : undefined,
      excludeUsHolidays: v.excludeUsHolidays,
      excludeDates: excludeDatesArr,
    };

    this.isGenerating.set(true);
    this.errorMessage.set(null);

    this.adminService.generateSchedule(request).subscribe({
      next: result => {
        this.schedulePreview.set(result);
        this.removedPreviewIndices.set(new Set());
        this.isGenerating.set(false);
      },
      error: err => {
        this.errorMessage.set(err.error?.message ?? 'Failed to generate schedule');
        this.isGenerating.set(false);
      }
    });
  }

  removePreviewGame(index: number): void {
    const removed = new Set(this.removedPreviewIndices());
    removed.add(index);
    this.removedPreviewIndices.set(removed);
  }

  confirmSchedule(): void {
    const preview = this.schedulePreview();
    if (!preview) return;

    const v = this.schedulerForm.value;
    const rinkId = this.selectedRinkId()!;
    const games = this.previewGames().map(g => ({
      gameDate: g.gameDate,
      homeTeamId: g.homeTeamId,
      awayTeamId: g.awayTeamId,
      rinkId,
      gameType: g.gameType ?? 'RegularSeason',
    }));

    if (games.length === 0) { this.errorMessage.set('No games to save'); return; }

    const request: ConfirmScheduleRequest = { sessionId: +v.sessionId, rinkId, games };
    this.isConfirming.set(true);

    this.adminService.confirmSchedule(request).subscribe({
      next: res => {
        this.successMessage.set(res.message);
        this.isConfirming.set(false);
        this.closeSchedulerPanel();
        this.loadMonthData();
        this.loadDayBookings();
        setTimeout(() => this.successMessage.set(null), 4000);
      },
      error: err => {
        this.errorMessage.set(err.error?.message ?? 'Failed to save schedule');
        this.isConfirming.set(false);
      }
    });
  }

  formatScheduleDate(dateStr: string): string {
    return new Date(dateStr).toLocaleString('en-US', { dateStyle: 'short', timeStyle: 'short' });
  }

  // ── Playoff methods ───────────────────────────────────────────────────────

  generatePlayoffPreview(): void {
    if (this.playoffForm.invalid) { this.playoffForm.markAllAsTouched(); return; }
    const sv = this.schedulerForm.value;
    if (!sv.sessionId) { this.errorMessage.set('Select a session before generating playoffs'); return; }

    const pv = this.playoffForm.value;
    const dowGroup = pv.daysOfWeek;
    const dowMap: Record<string, number> = { sun: 0, mon: 1, tue: 2, wed: 3, thu: 4, fri: 5, sat: 6 };
    const daysOfWeek = Object.entries(dowMap).filter(([k]) => dowGroup[k]).map(([, n]) => n);
    if (daysOfWeek.length === 0) { this.errorMessage.set('Select at least one day for playoffs'); return; }

    const excludeDatesArr = (pv.excludeDates as string)
      .split(/[\n,]+/).map((s: string) => s.trim()).filter((s: string) => s.length > 0);

    const rinkId = this.selectedRinkId();
    if (!rinkId) { this.errorMessage.set('Select a rink first'); return; }

    const request: GeneratePlayoffRequest = {
      sessionId: +sv.sessionId,
      rinkId,
      startDate: pv.startDate,
      endDate: pv.endDate,
      daysOfWeek,
      dailyStartTime: pv.dailyStartTime + ':00',
      dailyEndTime: pv.dailyEndTime + ':00',
      gameLengthMinutes: +pv.gameLengthMinutes,
      bufferMinutes: +pv.bufferMinutes,
      gamesPerNight: +pv.gamesPerNight,
      excludeUsHolidays: pv.excludeUsHolidays,
      excludeDates: excludeDatesArr,
    };

    this.isGeneratingPlayoffs.set(true);
    this.errorMessage.set(null);

    this.adminService.generatePlayoffSchedule(request).subscribe({
      next: result => {
        this.playoffPreview.set(result);
        this.removedPlayoffIndices.set(new Set());
        this.isGeneratingPlayoffs.set(false);
      },
      error: err => {
        this.errorMessage.set(err.error?.message ?? 'Failed to generate playoff schedule');
        this.isGeneratingPlayoffs.set(false);
      }
    });
  }

  removePlayoffGame(index: number): void {
    const removed = new Set(this.removedPlayoffIndices());
    removed.add(index);
    this.removedPlayoffIndices.set(removed);
  }

  confirmPlayoffs(): void {
    const preview = this.playoffPreview();
    if (!preview) return;

    const sv = this.schedulerForm.value;
    const rinkId = this.selectedRinkId()!;
    const games = this.playoffGames().map(g => ({
      gameDate: g.gameDate,
      homeTeamId: g.homeTeamId,
      awayTeamId: g.awayTeamId,
      rinkId,
      gameType: 'Playoff',
    }));

    if (games.length === 0) { this.errorMessage.set('No playoff games to save'); return; }

    const request: ConfirmScheduleRequest = { sessionId: +sv.sessionId, rinkId, games };
    this.isConfirmingPlayoffs.set(true);

    this.adminService.confirmSchedule(request).subscribe({
      next: res => {
        this.successMessage.set(res.message);
        this.isConfirmingPlayoffs.set(false);
        this.playoffPreview.set(null);
        this.loadMonthData();
        this.loadDayBookings();
        setTimeout(() => this.successMessage.set(null), 4000);
      },
      error: err => {
        this.errorMessage.set(err.error?.message ?? 'Failed to save playoff schedule');
        this.isConfirmingPlayoffs.set(false);
      }
    });
  }

  private toISODateStatic(date: Date): string {
    const y = date.getFullYear();
    const m = (date.getMonth() + 1).toString().padStart(2, '0');
    const d = date.getDate().toString().padStart(2, '0');
    return `${y}-${m}-${d}`;
  }

  // ── Helpers ────────────────────────────────────────────────────────────────

  formatHourLabel(hour: number): string {
    const h = hour % 24;
    if (h === 0) return '12:00 AM';
    if (h < 12) return `${h}:00 AM`;
    if (h === 12) return '12:00 PM';
    return `${h - 12}:00 PM`;
  }

  formatTime(dateStr: string): string {
    const d = new Date(dateStr);
    const h = d.getHours();
    const m = d.getMinutes().toString().padStart(2, '0');
    const period = h >= 12 ? 'PM' : 'AM';
    const hour = h % 12 || 12;
    return `${hour}:${m} ${period}`;
  }

  formatDate(date: Date): string {
    return date.toLocaleDateString('en-US', {
      weekday: 'long',
      month: 'long',
      day: 'numeric',
      year: 'numeric',
    });
  }

  formatMonthYear(date: Date): string {
    return date.toLocaleDateString('en-US', { month: 'long', year: 'numeric' });
  }

  toISODate(date: Date): string {
    const y = date.getFullYear();
    const m = (date.getMonth() + 1).toString().padStart(2, '0');
    const d = date.getDate().toString().padStart(2, '0');
    return `${y}-${m}-${d}`;
  }

  isSameDay(a: Date, b: Date): boolean {
    return (
      a.getFullYear() === b.getFullYear() &&
      a.getMonth() === b.getMonth() &&
      a.getDate() === b.getDate()
    );
  }

  getSlotBadgeClass(type: string): string {
    switch (type) {
      case 'session':
        return 'badge-session';
      case 'game':
        return 'badge-game';
      case 'blockout':
        return 'badge-blockout';
      default:
        return 'badge-available';
    }
  }

  getSlotIcon(type: string): string {
    switch (type) {
      case 'session':
        return 'bi-people-fill';
      case 'game':
        return 'bi-trophy-fill';
      case 'blockout':
        return 'bi-slash-circle-fill';
      default:
        return 'bi-clock';
    }
  }

  private toLocalDateTimeInput(date: Date): string {
    const y = date.getFullYear();
    const mo = (date.getMonth() + 1).toString().padStart(2, '0');
    const d = date.getDate().toString().padStart(2, '0');
    const h = date.getHours().toString().padStart(2, '0');
    const mi = date.getMinutes().toString().padStart(2, '0');
    return `${y}-${mo}-${d}T${h}:${mi}`;
  }

  // Kept for the single-click popover "Remove Blockout" button
  deleteBlockout(slot: CalendarSlot): void {
    const rinkId = this.selectedRinkId();
    if (!rinkId) return;
    if (!confirm(`Remove "${slot.title}"?`)) return;

    this.adminService.deleteBlockout(rinkId, slot.id).subscribe({
      next: () => {
        this.successMessage.set('Blockout removed');
        this.closeDetail();
        this.loadMonthData();
        this.loadDayBookings();
        setTimeout(() => this.successMessage.set(null), 3000);
      },
      error: (err) => {
        this.errorMessage.set(err.error?.message ?? 'Failed to remove blockout');
      },
    });
  }

  readonly weekdays = ['Sun', 'Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat'];
}
