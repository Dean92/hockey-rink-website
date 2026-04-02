import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, of } from 'rxjs';
import { catchError, map, tap } from 'rxjs/operators';
import { environment } from '../environments/environment';

@Injectable({
  providedIn: 'root',
})
export class AuthService {
  private apiUrl = `${environment.apiUrl}/auth`;
  private http = inject(HttpClient);

  getToken(): string | null {
    const token = localStorage.getItem('authToken');
    console.log('getToken called, token:', token);
    return token;
  }

  isAdmin(): boolean {
    const isAdmin = localStorage.getItem('isAdmin') === 'true';
    console.log('isAdmin called, result:', isAdmin);
    return isAdmin;
  }

  isSubAdmin(): boolean {
    return localStorage.getItem('isSubAdmin') === 'true';
  }

  isAnyAdmin(): boolean {
    return this.isAdmin() || this.isSubAdmin();
  }

  getPermissions(): string[] {
    try {
      return JSON.parse(localStorage.getItem('permissions') ?? '[]');
    } catch {
      return [];
    }
  }

  hasPermission(permission: string): boolean {
    if (this.isAdmin()) return true;
    return this.getPermissions().includes(permission);
  }

  getAuthHeaders() {
    const token = this.getToken();
    console.log('getAuthHeaders called, token:', token);
    return token ? { Authorization: `Bearer ${token}` } : undefined;
  }

  checkAuthStatus(): Observable<{ isValid: boolean }> {
    const token = this.getToken();
    console.log('checkAuthStatus called, token:', token);

    if (!token) {
      console.log('No token found, returning isValid: false');
      return of({ isValid: false });
    }

    const headers = this.getAuthHeaders();
    console.log('Calling validate endpoint with headers:', headers);

    return this.http
      .get<{ isValid: boolean }>(`${this.apiUrl}/validate`, {
        ...(headers && { headers }),
        withCredentials: true,
      })
      .pipe(
        tap((response) => console.log('Auth validation response:', response)),
        map((response) => ({ isValid: response.isValid })),
        catchError((err) => {
          console.error('Auth validation failed:', err);
          return of({ isValid: false });
        }),
      );
  }

  login(email: string, password: string): Observable<any> {
    console.log('Login attempt for:', email);
    return this.http
      .post<{
        token: string;
        isAdmin: boolean;
        isSubAdmin: boolean;
        permissions: string[];
      }>(`${this.apiUrl}/login`, { email, password }, { withCredentials: true })
      .pipe(
        tap((response) => console.log('Login response:', response)),
        map((response) => {
          localStorage.setItem('authToken', response.token);
          localStorage.setItem('isAdmin', response.isAdmin.toString());
          localStorage.setItem('isSubAdmin', (response.isSubAdmin ?? false).toString());
          localStorage.setItem('permissions', JSON.stringify(response.permissions ?? []));
          return response;
        }),
        catchError((err) => {
          console.error('Login failed:', err);
          throw err;
        }),
      );
  }

  register(
    firstName: string,
    lastName: string,
    email: string,
    phone: string,
    dateOfBirth: string,
    address: string,
    city: string,
    state: string,
    zipCode: string,
    password: string,
    position: string,
    emergencyContactName: string,
    emergencyContactPhone: string,
  ): Observable<any> {
    console.log('Register attempt for:', email);
    return this.http
      .post<{ token: string }>(
        `${this.apiUrl}/register`,
        {
          firstName,
          lastName,
          email,
          phone,
          dateOfBirth,
          address,
          city,
          state,
          zipCode,
          password,
          position,
          emergencyContactName,
          emergencyContactPhone,
        },
        { withCredentials: true },
      )
      .pipe(
        tap((response) => console.log('Register response:', response)),
        map((response) => {
          console.log('Storing token:', response.token);
          localStorage.setItem('authToken', response.token);
          localStorage.setItem('isAdmin', 'false'); // New users are not admin
          return response;
        }),
        catchError((err) => {
          console.error('Registration failed:', err);
          throw err;
        }),
      );
  }

  logout(): void {
    console.log('Logging out, removing token and admin status');
    localStorage.removeItem('authToken');
    localStorage.removeItem('isAdmin');
    localStorage.removeItem('isSubAdmin');
    localStorage.removeItem('permissions');
  }
}
