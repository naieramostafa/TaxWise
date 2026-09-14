import { Injectable, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { tap } from 'rxjs';
import { environment } from '../../environments/environment';

interface AuthResponse {
  token: string;
  refreshToken: string;
  userId: string;
  email: string;
  name: string;
}

@Injectable({ providedIn: 'root' })
export class AuthService {
  private baseUrl = `${environment.apiUrl}/api/auth`;
  private tokenKey = 'auth_token';
  private refreshKey = 'refresh_token';
  private userIdKey = 'user_id';
  private emailKey = 'user_email';
  private nameKey = 'user_name';

  token = signal<string | null>(localStorage.getItem(this.tokenKey));
  refreshToken = signal<string | null>(localStorage.getItem(this.refreshKey));
  userId = signal<string | null>(localStorage.getItem(this.userIdKey));
  email = signal<string | null>(localStorage.getItem(this.emailKey));
  name = signal<string | null>(localStorage.getItem(this.nameKey));

  displayName(): string {
    return this.name() || this.email() || 'User';
  }

  constructor(
    private http: HttpClient,
    private router: Router
  ) {}

  register(email: string, password: string, name: string) {
    return this.http.post<AuthResponse>(`${this.baseUrl}/register`, { email, password, name })
      .pipe(tap(r => this.setSession(r)));
  }

  login(email: string, password: string) {
    return this.http.post<AuthResponse>(`${this.baseUrl}/login`, { email, password })
      .pipe(tap(r => this.setSession(r)));
  }
  refreshTokens() {
    const rt = this.refreshToken();
    if (!rt) return null;
    return this.http.post<AuthResponse>(`${this.baseUrl}/refresh`, { refreshToken: rt })
      .pipe(tap(r => this.setSession(r)));
  }

  requestPasswordReset(email: string) {
    return this.http.post(`${this.baseUrl}/forgot-password`, { email });
  }

  resetPassword(email: string, token: string, newPassword: string) {
    return this.http.post(`${this.baseUrl}/reset-password`, { email, token, newPassword });
  }

  requestEmailVerification(email: string) {
    return this.http.post(`${this.baseUrl}/verify-email-request`, { email });
  }

  confirmEmail(email: string, token: string) {
    return this.http.post(`${this.baseUrl}/confirm-email`, { email, token });
  }

  updateProfile(name: string) {
    return this.http.put<{ name: string }>(`${this.baseUrl}/profile`, { name })
      .pipe(tap(r => this.setName(r.name)));
  }

  private setName(name: string) {
    localStorage.setItem(this.nameKey, name);
    this.name.set(name);
  }

  logout() {
    if (this.token()) {
      this.http.post(`${this.baseUrl}/logout`, {}).subscribe();
    }
    localStorage.removeItem(this.tokenKey);
    localStorage.removeItem(this.refreshKey);
    localStorage.removeItem(this.userIdKey);
    localStorage.removeItem(this.emailKey);
    localStorage.removeItem(this.nameKey);
    this.token.set(null);
    this.refreshToken.set(null);
    this.userId.set(null);
    this.email.set(null);
    this.name.set(null);
    this.router.navigate(['/login']);
  }

  private setSession(r: AuthResponse) {
    localStorage.setItem(this.tokenKey, r.token);
    localStorage.setItem(this.refreshKey, r.refreshToken);
    localStorage.setItem(this.userIdKey, r.userId);
    localStorage.setItem(this.emailKey, r.email);
    localStorage.setItem(this.nameKey, r.name);
    this.token.set(r.token);
    this.refreshToken.set(r.refreshToken);
    this.userId.set(r.userId);
    this.email.set(r.email);
    this.name.set(r.name);
  }
}
