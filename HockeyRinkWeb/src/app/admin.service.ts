import { Injectable, inject } from "@angular/core";
import { HttpClient } from "@angular/common/http";
import { Observable } from "rxjs";
import { AuthService } from "./auth";
import { environment } from "../environments/environment";

export interface AdminDashboardData {
  totalUsers: number;
  totalSessions: number;
  totalRegistrations: number;
  totalRevenue: number;
  recentRegistrations: RecentRegistration[];
}

export interface RecentRegistration {
  id: number;
  userName: string;
  userEmail: string;
  sessionName: string;
  paymentStatus: string;
  createdAt: Date;
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
}

@Injectable({
  providedIn: "root",
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
}
