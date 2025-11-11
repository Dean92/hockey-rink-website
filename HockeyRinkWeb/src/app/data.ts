import { Injectable, inject } from "@angular/core";
import { HttpClient, HttpParams } from "@angular/common/http";
import { Observable, of, tap, catchError } from "rxjs";
import { AuthService } from "./auth";
import { environment } from "../environments/environment";
import { League, Session } from "./models";

@Injectable({
  providedIn: "root",
})
export class DataService {
  private apiUrl = environment.apiUrl;
  private http = inject(HttpClient);
  private authService = inject(AuthService);

  getLeagues(): Observable<League[]> {
    const headers = this.authService.getAuthHeaders();
    return this.http
      .get<League[]>(`${this.apiUrl}/leagues`, {
        headers,
        withCredentials: true,
      })
      .pipe(
        catchError((err) => {
          console.error("Error fetching leagues:", err);
          return of([]);
        })
      );
  }

  getSessions(leagueId?: number, date?: Date): Observable<Session[]> {
    const headers = this.authService.getAuthHeaders();
    let url = `${this.apiUrl}/sessions`;
    if (leagueId || date) {
      let params = new HttpParams();
      if (leagueId) params = params.set("leagueId", leagueId.toString());
      if (date) params = params.set("date", date.toISOString());
      url += `?${params.toString()}`;
    }
    return this.http
      .get<Session[]>(url, { headers, withCredentials: true })
      .pipe(
        catchError((err) => {
          console.error("Error fetching sessions:", err);
          return of([]);
        })
      );
  }

  registerSession(sessionId: number): Observable<any> {
    const headers = this.authService.getAuthHeaders();
    return this.http.post(
      `${this.apiUrl}/sessions/register`,
      { sessionId },
      { headers, withCredentials: true }
    );
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
}
