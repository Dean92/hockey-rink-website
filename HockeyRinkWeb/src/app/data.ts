import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, of, tap, catchError } from 'rxjs';
import { map } from 'rxjs/operators';
import { AuthService } from './auth';
import { environment } from '../environments/environment';
import { League, Session, SessionRegistrationRequest } from './models';

@Injectable({
  providedIn: 'root',
})
export class DataService {
  private apiUrl = environment.apiUrl;
  private http = inject(HttpClient);
  private authService = inject(AuthService);

  getLeagues(): Observable<League[]> {
    // Public endpoint - no authentication required
    return this.http
      .get<League[]>(`${this.apiUrl}/leagues`, {
        withCredentials: true,
      })
      .pipe(
        catchError((err) => {
          console.error('Error fetching leagues:', err);
          return of([]);
        })
      );
  }

  getSessions(leagueId?: number, date?: Date): Observable<Session[]> {
    const headers = this.authService.getAuthHeaders();
    let url = `${this.apiUrl}/sessions`;
    if (leagueId || date) {
      let params = new HttpParams();
      if (leagueId) params = params.set('leagueId', leagueId.toString());
      if (date) params = params.set('date', date.toISOString());
      url += `?${params.toString()}`;
    }
    return this.http
      .get<Session[]>(url, { headers, withCredentials: true })
      .pipe(
        catchError((err) => {
          console.error('Error fetching sessions:', err);
          return of([]);
        })
      );
  }

  registerSession(registration: SessionRegistrationRequest): Observable<any> {
    const headers = this.authService.getAuthHeaders();
    return this.http.post(`${this.apiUrl}/sessions/register`, registration, {
      headers,
      withCredentials: true,
    });
  }

  getProfile(): Observable<any> {
    const headers = this.authService.getAuthHeaders();
    return this.http.get(`${this.apiUrl}/users/profile`, {
      headers,
      withCredentials: true,
    });
  }

  getDashboard(): Observable<any> {
    const headers = this.authService.getAuthHeaders();
    return this.http.get(`${this.apiUrl}/users/dashboard`, {
      headers,
      withCredentials: true,
    });
  }

  // Admin methods
  getAllSessions(): Observable<Session[]> {
    const headers = this.authService.getAuthHeaders();
    return this.http.get<Session[]>(`${this.apiUrl}/admin/sessions/all`, {
      headers,
      withCredentials: true,
    });
  }

  createSession(session: Partial<Session>): Observable<any> {
    const headers = this.authService.getAuthHeaders();
    return this.http.post(`${this.apiUrl}/admin/sessions`, session, {
      headers,
      withCredentials: true,
    });
  }

  updateSession(id: number, session: Partial<Session>): Observable<any> {
    const headers = this.authService.getAuthHeaders();
    return this.http.put(`${this.apiUrl}/admin/sessions/${id}`, session, {
      headers,
      withCredentials: true,
    });
  }

  deleteSession(id: number): Observable<any> {
    const headers = this.authService.getAuthHeaders();
    return this.http.delete(`${this.apiUrl}/admin/sessions/${id}`, {
      headers,
      withCredentials: true,
    });
  }

  getAllLeagues(): Observable<League[]> {
    const headers = this.authService.getAuthHeaders();
    return this.http.get<League[]>(`${this.apiUrl}/admin/leagues`, {
      headers,
      withCredentials: true,
    });
  }

  updateLeague(id: number, league: Partial<League>): Observable<any> {
    const headers = this.authService.getAuthHeaders();
    return this.http.put(`${this.apiUrl}/admin/leagues/${id}`, league, {
      headers,
      withCredentials: true,
    });
  }

  // Registration management methods
  getSessionRegistrations(sessionId: number): Observable<any> {
    const headers = this.authService.getAuthHeaders();
    return this.http.get<any>(
      `${this.apiUrl}/admin/sessions/${sessionId}/registrations`,
      {
        headers,
        withCredentials: true,
      }
    );
  }

  addManualRegistration(sessionId: number, data: any): Observable<any> {
    const headers = this.authService.getAuthHeaders();
    return this.http.post(
      `${this.apiUrl}/admin/sessions/${sessionId}/registrations/manual`,
      data,
      {
        headers,
        withCredentials: true,
      }
    );
  }

  updateRegistration(
    sessionId: number,
    registrationId: number,
    data: any
  ): Observable<any> {
    const headers = this.authService.getAuthHeaders();
    return this.http.put(
      `${this.apiUrl}/admin/sessions/${sessionId}/registrations/${registrationId}`,
      data,
      {
        headers,
        withCredentials: true,
      }
    );
  }

  removeRegistration(
    sessionId: number,
    registrationId: number
  ): Observable<any> {
    const headers = this.authService.getAuthHeaders();
    return this.http.delete(
      `${this.apiUrl}/admin/sessions/${sessionId}/registrations/${registrationId}`,
      {
        headers,
        withCredentials: true,
      }
    );
  }

  validatePasswordSetupToken(token: string): Observable<any> {
    return this.http.get(`${this.apiUrl}/auth/setup-password/${token}`, {
      withCredentials: true,
    });
  }

  setupPassword(data: {
    token: string;
    password: string;
    confirmPassword: string;
  }): Observable<any> {
    return this.http.post(`${this.apiUrl}/auth/setup-password`, data, {
      withCredentials: true,
    });
  }

  updateProfile(data: {
    address?: string;
    city?: string;
    state?: string;
    zipCode?: string;
    phone?: string;
    dateOfBirth?: string;
    position?: string;
  }): Observable<any> {
    const headers = this.authService.getAuthHeaders();
    return this.http.put(`${this.apiUrl}/users/profile`, data, {
      headers,
      withCredentials: true,
    });
  }

  getMySessions(): Observable<any> {
    const headers = this.authService.getAuthHeaders();
    return this.http.get(`${this.apiUrl}/users/my-sessions`, {
      headers,
      withCredentials: true,
    });
  }

  changePassword(data: {
    currentPassword: string;
    newPassword: string;
    confirmPassword: string;
  }): Observable<any> {
    const headers = this.authService.getAuthHeaders();
    return this.http.post(`${this.apiUrl}/users/change-password`, data, {
      headers,
      withCredentials: true,
    });
  }

  cancelSessionRegistration(registrationId: number): Observable<any> {
    const headers = this.authService.getAuthHeaders();
    return this.http.delete(
      `${this.apiUrl}/users/my-sessions/${registrationId}`,
      {
        headers,
        withCredentials: true,
      }
    );
  }

  // Team management methods
  getTeamsForSession(sessionId: number): Observable<any[]> {
    const headers = this.authService.getAuthHeaders();
    return this.http.get<any[]>(
      `${this.apiUrl}/admin/sessions/${sessionId}/teams`,
      {
        ...(headers && { headers }),
        withCredentials: true,
      }
    );
  }

  createTeam(sessionId: number, team: any): Observable<any> {
    const headers = this.authService.getAuthHeaders();
    return this.http.post(
      `${this.apiUrl}/admin/sessions/${sessionId}/teams`,
      team,
      {
        ...(headers && { headers }),
        withCredentials: true,
      }
    );
  }

  updateTeam(teamId: number, team: any): Observable<any> {
    const headers = this.authService.getAuthHeaders();
    return this.http.put(
      `${this.apiUrl}/admin/sessions/teams/${teamId}`,
      team,
      {
        ...(headers && { headers }),
        withCredentials: true,
      }
    );
  }

  deleteTeam(teamId: number): Observable<any> {
    const headers = this.authService.getAuthHeaders();
    return this.http.delete(`${this.apiUrl}/admin/sessions/teams/${teamId}`, {
      headers,
      withCredentials: true,
    });
  }

  getSessionById(sessionId: number): Observable<any> {
    const headers = this.authService.getAuthHeaders();
    return this.http.get(`${this.apiUrl}/admin/sessions/${sessionId}`, {
      headers,
      withCredentials: true,
    });
  }
}
