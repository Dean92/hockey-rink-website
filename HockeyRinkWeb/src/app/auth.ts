import { Injectable } from "@angular/core";
import { HttpClient } from "@angular/common/http";
import { Observable } from "rxjs";

@Injectable({
  providedIn: "root",
})
export class AuthService {
  private apiUrl = "https://localhost:7134/api/auth"; // Replace with your API URL
  constructor(private http: HttpClient) {}

  login(email: string, password: string): Observable<any> {
    return this.http.post<any>(`${this.apiUrl}/login`, {
      email,
      password,
      rememberMe: true,
    });
  }

  register(
    firstName: string,
    lastName: string,
    email: string,
    password: string
  ): Observable<any> {
    return this.http.post<any>(`${this.apiUrl}/register`, {
      firstName,
      lastName,
      email,
      password,
    });
  }
}
