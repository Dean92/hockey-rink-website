import { Injectable } from "@angular/core";
import { HttpClient } from "@angular/common/http";
import { Observable } from "rxjs";
import { AuthService } from "./auth";

@Injectable({
  providedIn: "root",
})
export class DataService {
  private apiUrl =
    "https://hockey-rink-api-bbhch3gwgzedc9e3.centralus-01.azurewebsites.net/api";

  constructor(private http: HttpClient, private authService: AuthService) {}

  getLeagues(): Observable<any> {
    const headers = this.authService.getAuthHeaders();
    return this.http.get(`${this.apiUrl}/leagues`, {
      headers,
      withCredentials: true,
    });
  }

  getSessions(): Observable<any> {
    const headers = this.authService.getAuthHeaders();
    return this.http.get(`${this.apiUrl}/sessions`, {
      headers,
      withCredentials: true,
    });
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
}
