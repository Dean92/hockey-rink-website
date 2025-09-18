import { Injectable } from "@angular/core";
import { HttpClient } from "@angular/common/http";
import { Observable } from "rxjs";
import { AuthService } from "./auth";

@Injectable({
  providedIn: "root",
})
export class DataService {
  private apiUrl = "https://localhost:7134/api";

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
}
