import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { BehaviorSubject, Observable } from 'rxjs';
import { tap } from 'rxjs/operators';

import { environment } from '../../../environments/environment';
import { TokenService } from './token.service';
import { Usuario, RegistroUsuario } from '../../features/auth/models/usuario.model';
import { LoginCredentials } from '../../features/auth/models/login.model';
import { TokenResponse } from '../../features/auth/models/token-response.model';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private readonly apiUrl = `${environment.apiUrl}/api/auth`;

  private currentUserSubject = new BehaviorSubject<Usuario | null>(null);
  currentUser$ = this.currentUserSubject.asObservable();

  constructor(
    private http: HttpClient,
    private tokenService: TokenService,
    private router: Router
  ) {}

  register(data: RegistroUsuario): Observable<Usuario> {
    return this.http.post<Usuario>(`${this.apiUrl}/register`, data);
  }

  login(credentials: LoginCredentials): Observable<TokenResponse> {
    return this.http.post<TokenResponse>(`${this.apiUrl}/login`, credentials).pipe(
      tap((response) => {
        console.log('[AuthService] Login response:', response);
        this.tokenService.saveTokens(response.accessToken, response.refreshToken);
        this.loadCurrentUser();
      })
    );
  }

  logout(): void {
    this.tokenService.clearTokens();
    this.currentUserSubject.next(null);
    this.router.navigate(['/login']);
  }

  refreshToken(): Observable<TokenResponse> {
    const refreshToken = this.tokenService.getRefreshToken();
    return this.http.post<TokenResponse>(`${this.apiUrl}/refresh`, { refreshToken }).pipe(
      tap((response) => {
        this.tokenService.saveTokens(response.accessToken, response.refreshToken);
      })
    );
  }

  forgotPassword(email: string): Observable<void> {
    return this.http.post<void>(`${this.apiUrl}/forgot-password`, { email });
  }

  resetPassword(token: string, email: string, nuevaPassword: string, confirmarPassword: string): Observable<void> {
    return this.http.post<void>(`${this.apiUrl}/reset-password`, {
      token,
      email,
      nuevaPassword,
      confirmarPassword
    });
  }

  isAuthenticated(): boolean {
    return this.tokenService.hasValidToken();
  }

  loadCurrentUser(): void {
    const token = this.tokenService.getAccessToken();
    if (!token) {
      this.currentUserSubject.next(null);
      return;
    }

    try {
      const decoded = this.tokenService.decodeToken(token);
      const user: Usuario = {
        id: decoded.sub,
        nombres: decoded.nombres,
        apellidos: decoded.apellidos,
        cedula: decoded.cedula ?? '',
        email: decoded.email
      };
      this.currentUserSubject.next(user);
    } catch {
      this.currentUserSubject.next(null);
    }
  }
}
