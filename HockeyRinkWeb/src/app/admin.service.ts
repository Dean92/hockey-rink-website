import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { AuthService } from './auth';
import { environment } from '../environments/environment';

export interface AdminDashboardData {
  todaysRegistrationsCount: number;
  activeSessionsCount: number;
  totalRevenue: number;
  yearRevenue: number;
  revenueYear: number;
  monthRevenue: number;
  activeSessions: ActiveSessionSummary[];
  upcomingSessions: UpcomingSession[];
  recentRegistrations: RecentRegistration[];
}

export interface ActiveSessionSummary {
  id: number;
  name: string;
  leagueName: string | null;
  startDate: string;
  endDate: string;
  maxPlayers: number;
  registeredCount: number;
  spotsRemaining: number;
  totalRevenue: number;
  regularPrice: number;
}

export interface UpcomingSession {
  id: number;
  name: string;
  leagueName: string | null;
  startDate: string;
  endDate: string;
  registeredCount: number;
  maxPlayers: number;
}

export interface RecentRegistration {
  id: number;
  name: string;
  email: string;
  sessionId: number;
  registrationDate: string;
  amountPaid: number;
}

export interface AdminUser {
  id: string;
  firstName: string;
  lastName: string;
  email: string;
  leagueId?: number;
  leagueName?: string;
  emailConfirmed: boolean;
  createdAt: Date;
  rating?: number;
  playerNotes?: string;
  position?: string;
  address?: string;
  city?: string;
  state?: string;
  zipCode?: string;
  phone?: string;
  dateOfBirth?: string;
  lastLoginAt?: string;
  emergencyContactName?: string;
  emergencyContactPhone?: string;
  hockeyRegistrationNumber?: string;
  hockeyRegistrationType?: string;
}

export interface AdminSession {
  id: number;
  name: string;
  startDate: Date;
  endDate: Date;
  fee: number;
  isActive: boolean;
  leagueId?: number;
  leagueName?: string;
  registrationCount: number;
  createdAt: Date;
  regularSeasonGames?: number;
}

export interface AdminRegistration {
  id: number;
  userId: string;
  userName: string;
  userEmail: string;
  sessionId: number;
  sessionName: string;
  leagueName?: string;
  paymentStatus: string;
  paymentDate: Date;
  totalPaid: number;
  createdAt: Date;
}

export interface CreateSessionRequest {
  name: string;
  startDate: Date;
  endDate: Date;
  fee: number;
  isActive: boolean;
  leagueId?: number;
  regularSeasonGames?: number;
}

// ── Rink Management ──────────────────────────────────────────────────────────

export interface Rink {
  id: number;
  name: string;
  description?: string;
  isActive: boolean;
  createdAt: string;
}

export interface RinkBlockout {
  id: number;
  rinkId: number;
  startDateTime: string;
  endDateTime: string;
  reason?: string;
  createdAt: string;
}

export interface CreateBlockoutRequest {
  startDateTime: string;
  endDateTime: string;
  reason?: string;
}

// ── Rink Calendar ─────────────────────────────────────────────────────────────

export interface CalendarSlot {
  id: number;
  type: 'session' | 'game' | 'blockout';
  title: string;
  startDateTime: string;
  endDateTime: string;
  rinkId: number;
  rinkName?: string;
  leagueName?: string;
  homeTeamName?: string;
  awayTeamName?: string;
  status?: string;
  reason?: string;
}

export interface DayBookingsResponse {
  rinkId: number;
  rinkName: string;
  date: string;
  slots: CalendarSlot[];
}

export interface MonthBookingCountsResponse {
  rinkId: number;
  year: number;
  month: number;
  dayCounts: Record<number, number>;
}

export interface ConflictCheckRequest {
  rinkId: number;
  startDateTime: string;
  endDateTime: string;
  excludeGameId?: number;
}

export interface ConflictCheckResponse {
  hasConflict: boolean;
  conflictType?: string;
  conflictTitle?: string;
  conflictStart?: string;
  conflictEnd?: string;
}

// ── League Games ──────────────────────────────────────────────────────────────

export interface GameSummary {
  id: number;
  gameDate: string;
  sessionId: number;
  sessionName: string;
  rinkId?: number;
  rinkName?: string;
  homeTeamId: number;
  homeTeamName: string;
  awayTeamId: number;
  awayTeamName: string;
  homeScore?: number;
  awayScore?: number;
  status: string;
  location?: string;
  gameType: string;
}

export interface UpdateGameRequest {
  gameDate: string;
  rinkId?: number;
  homeTeamId: number;
  awayTeamId: number;
  homeScore?: number;
  awayScore?: number;
  status?: string;
  location?: string;
  excludeGameId?: number;
}

// ── Schedule Generator ────────────────────────────────────────────────────

export interface GenerateScheduleRequest {
  leagueId: number;
  sessionId: number;
  rinkId: number;
  startDate: string;
  endDate: string;
  daysOfWeek: number[];
  dailyStartTime: string;
  dailyEndTime: string;
  gameLengthMinutes: number;
  bufferMinutes: number;
  gamesPerNight: number;
  gamesPerMatchup: number;
  totalGamesPerTeam?: number;
  excludeUsHolidays: boolean;
  excludeDates: string[];
}

export interface GeneratePlayoffRequest {
  sessionId: number;
  rinkId: number;
  startDate: string;
  endDate: string;
  daysOfWeek: number[];
  dailyStartTime: string;
  dailyEndTime: string;
  gameLengthMinutes: number;
  bufferMinutes: number;
  gamesPerNight: number;
  excludeUsHolidays: boolean;
  excludeDates: string[];
}

export interface ProposedGame {
  gameDate: string;
  homeTeamId: number;
  homeTeamName: string;
  awayTeamId: number;
  awayTeamName: string;
  rinkId: number;
  hasConflict: boolean;
  conflictReason?: string;
  gameType: string;
}

export interface SkippedDate {
  date: string;
  reason: string;
}

export interface UnscheduledMatchup {
  homeTeamId: number;
  homeTeamName: string;
  awayTeamId: number;
  awayTeamName: string;
  reason: string;
}

export interface GenerateScheduleResponse {
  proposedGames: ProposedGame[];
  skippedDates: SkippedDate[];
  unscheduledMatchups: UnscheduledMatchup[];
  totalGamesGenerated: number;
}

export interface ConfirmScheduleRequest {
  sessionId: number;
  rinkId: number;
  games: ConfirmGameItem[];
}

export interface ConfirmGameItem {
  gameDate: string;
  homeTeamId: number;
  awayTeamId: number;
  rinkId: number;
  gameType: string;
}

@Injectable({
  providedIn: 'root',
})
export class AdminService {
  private apiUrl = `${environment.apiUrl}/admin`;
  private http = inject(HttpClient);
  private authService = inject(AuthService);

  getDashboard(): Observable<AdminDashboardData> {
    const headers = this.authService.getAuthHeaders();
    return this.http.get<AdminDashboardData>(`${this.apiUrl}/dashboard`, {
      headers,
      withCredentials: true,
    });
  }

  getUsers(): Observable<AdminUser[]> {
    const headers = this.authService.getAuthHeaders();
    return this.http.get<AdminUser[]>(`${this.apiUrl}/users`, {
      headers,
      withCredentials: true,
    });
  }

  updatePlayerRating(
    userId: string,
    rating: number | null,
    playerNotes: string | null,
  ): Observable<any> {
    const headers = this.authService.getAuthHeaders();
    return this.http.put(
      `${this.apiUrl}/users/${userId}/rating`,
      {
        rating,
        playerNotes,
      },
      {
        headers,
        withCredentials: true,
      },
    );
  }

  getSessions(): Observable<AdminSession[]> {
    const headers = this.authService.getAuthHeaders();
    return this.http.get<AdminSession[]>(`${this.apiUrl}/sessions/all`, {
      headers,
      withCredentials: true,
    });
  }

  createSession(session: CreateSessionRequest): Observable<any> {
    const headers = this.authService.getAuthHeaders();
    return this.http.post(`${this.apiUrl}/sessions`, session, {
      headers,
      withCredentials: true,
    });
  }

  updateSession(id: number, session: CreateSessionRequest): Observable<any> {
    const headers = this.authService.getAuthHeaders();
    return this.http.put(`${this.apiUrl}/sessions/${id}`, session, {
      headers,
      withCredentials: true,
    });
  }

  deleteSession(id: number): Observable<any> {
    const headers = this.authService.getAuthHeaders();
    return this.http.delete(`${this.apiUrl}/sessions/${id}`, {
      headers,
      withCredentials: true,
    });
  }

  getRegistrations(): Observable<AdminRegistration[]> {
    const headers = this.authService.getAuthHeaders();
    return this.http.get<AdminRegistration[]>(`${this.apiUrl}/registrations`, {
      headers,
      withCredentials: true,
    });
  }

  updateUserProfile(userId: string, profile: any): Observable<any> {
    const headers = this.authService.getAuthHeaders();
    return this.http.put(`${this.apiUrl}/users/${userId}/profile`, profile, {
      headers,
      withCredentials: true,
    });
  }

  // ── Rink Management ──────────────────────────────────────────────────────────

  getRinks(): Observable<Rink[]> {
    const headers = this.authService.getAuthHeaders();
    return this.http.get<Rink[]>(`${this.apiUrl}/rinks`, { headers, withCredentials: true });
  }

  createBlockout(rinkId: number, request: CreateBlockoutRequest): Observable<any> {
    const headers = this.authService.getAuthHeaders();
    return this.http.post(`${this.apiUrl}/rinks/${rinkId}/blockouts`, request, { headers, withCredentials: true });
  }

  deleteBlockout(rinkId: number, blockoutId: number): Observable<any> {
    const headers = this.authService.getAuthHeaders();
    return this.http.delete(`${this.apiUrl}/rinks/${rinkId}/blockouts/${blockoutId}`, { headers, withCredentials: true });
  }

  // ── Rink Calendar ─────────────────────────────────────────────────────────────

  getRinkDayBookings(rinkId: number, date: string): Observable<DayBookingsResponse> {
    const headers = this.authService.getAuthHeaders();
    return this.http.get<DayBookingsResponse>(`${this.apiUrl}/rink-calendar`, {
      headers,
      withCredentials: true,
      params: { rinkId: rinkId.toString(), date }
    });
  }

  getRinkMonthCounts(rinkId: number, year: number, month: number): Observable<MonthBookingCountsResponse> {
    const headers = this.authService.getAuthHeaders();
    return this.http.get<MonthBookingCountsResponse>(`${this.apiUrl}/rink-calendar/month`, {
      headers,
      withCredentials: true,
      params: { rinkId: rinkId.toString(), year: year.toString(), month: month.toString() }
    });
  }

  checkConflict(request: ConflictCheckRequest): Observable<ConflictCheckResponse> {
    const headers = this.authService.getAuthHeaders();
    return this.http.post<ConflictCheckResponse>(`${this.apiUrl}/rink-calendar/check-conflict`, request, { headers, withCredentials: true });
  }

  // ── League Games ──────────────────────────────────────────────────────────────

  getLeagueGames(leagueId: number, filters?: {
    teamId?: number;
    status?: string;
    startDate?: string;
    endDate?: string;
    rinkId?: number;
  }): Observable<GameSummary[]> {
    const headers = this.authService.getAuthHeaders();
    const params: Record<string, string> = {};
    if (filters?.teamId) params['teamId'] = filters.teamId.toString();
    if (filters?.status) params['status'] = filters.status;
    if (filters?.startDate) params['startDate'] = filters.startDate;
    if (filters?.endDate) params['endDate'] = filters.endDate;
    if (filters?.rinkId) params['rinkId'] = filters.rinkId.toString();
    return this.http.get<GameSummary[]>(`${this.apiUrl}/leagues/${leagueId}/games`, { headers, withCredentials: true, params });
  }

  updateGame(gameId: number, request: UpdateGameRequest): Observable<GameSummary> {
    const headers = this.authService.getAuthHeaders();
    return this.http.put<GameSummary>(`${this.apiUrl}/games/${gameId}`, request, { headers, withCredentials: true });
  }

  cancelGame(gameId: number): Observable<any> {
    const headers = this.authService.getAuthHeaders();
    return this.http.put(`${this.apiUrl}/games/${gameId}/cancel`, {}, { headers, withCredentials: true });
  }

  // ── Schedule Generator ────────────────────────────────────────────────────

  generateSchedule(request: GenerateScheduleRequest): Observable<GenerateScheduleResponse> {
    const headers = this.authService.getAuthHeaders();
    return this.http.post<GenerateScheduleResponse>(`${this.apiUrl}/schedule/generate`, request, { headers, withCredentials: true });
  }

  generatePlayoffSchedule(request: GeneratePlayoffRequest): Observable<GenerateScheduleResponse> {
    const headers = this.authService.getAuthHeaders();
    return this.http.post<GenerateScheduleResponse>(`${this.apiUrl}/schedule/generate-playoffs`, request, { headers, withCredentials: true });
  }

  confirmSchedule(request: ConfirmScheduleRequest): Observable<{ message: string; count: number }> {
    const headers = this.authService.getAuthHeaders();
    return this.http.post<{ message: string; count: number }>(`${this.apiUrl}/schedule/confirm`, request, { headers, withCredentials: true });
  }
}
