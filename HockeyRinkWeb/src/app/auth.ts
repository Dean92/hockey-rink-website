import { Injectable } from "@angular/core";
import { HttpClient, HttpHeaders } from "@angular/common/http";
import { Observable, tap } from "rxjs";

@Injectable({
  providedIn: "root",
})
export class AuthService {
  private apiUrl = "https://localhost:7134/api/auth";
  private readonly TOKEN_KEY = "auth_token";

  constructor(private http: HttpClient) {}

  login(email: string, password: string): Observable<any> {
    return this.http
      .post<any>(
        `${this.apiUrl}/login`,
        {
          email,
          password,
          rememberMe: true,
        },
        { withCredentials: true }
      )
      .pipe(
        tap((response) => {
          if (response.token) {
            this.setToken(response.token);
          }
        })
      );
  }

  register(
    firstName: string,
    lastName: string,
    email: string,
    password: string
  ): Observable<any> {
    return this.http.post<any>(
      `${this.apiUrl}/register`,
      {
        firstName,
        lastName,
        email,
        password,
      },
      { withCredentials: true }
    );
  }

  checkAuthStatus(): Observable<any> {
    const token = this.getToken();

    if (token) {
      return this.http.post<any>(
        `${this.apiUrl}/validate-token`,
        { Token: token },
        { withCredentials: true }
      );
    }

    return this.http.get<any>(`${this.apiUrl}/status`, {
      withCredentials: true,
    });
  }

  private setToken(token: string): void {
    localStorage.setItem(this.TOKEN_KEY, token);
  }

  getToken(): string | null {
    return localStorage.getItem(this.TOKEN_KEY);
  }

  getAuthHeaders(): HttpHeaders {
    const token = this.getToken();
    if (token) {
      return new HttpHeaders().set("Authorization", `Bearer ${token}`);
    }
    return new HttpHeaders();
  }

  logout(): void {
    localStorage.removeItem(this.TOKEN_KEY);
  }
}
