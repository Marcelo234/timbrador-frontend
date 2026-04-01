import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { BehaviorSubject, Observable, throwError } from 'rxjs';
import { catchError, filter, switchMap, take, tap } from 'rxjs/operators';

import { TokenService } from '../services/token.service';
import { AuthService } from '../services/auth.service';
import { TokenResponse } from '../../features/auth/models/token-response.model';

// Auth endpoints never need token refresh on 401
const AUTH_ENDPOINTS = ['/api/auth/login', '/api/auth/register', '/api/auth/refresh'];

// ─── Refresh lock (module-level, shared across all interceptor invocations) ───
let isRefreshing = false;
const refreshToken$ = new BehaviorSubject<string | null>(null);

/** Clones a request adding the ngrok bypass header and optionally a Bearer token. */
function addHeaders(req: Parameters<HttpInterceptorFn>[0], token?: string | null) {
  const headers: Record<string, string> = { 'ngrok-skip-browser-warning': 'true' };
  if (token) headers['Authorization'] = `Bearer ${token}`;
  return req.clone({ setHeaders: headers });
}

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const tokenService = inject(TokenService);
  const authService  = inject(AuthService);
  const router       = inject(Router);

  const accessToken = tokenService.getAccessToken();
  const authReq = addHeaders(req, accessToken);

  const isAuthEndpoint = AUTH_ENDPOINTS.some(ep => req.url.includes(ep));

  return next(authReq).pipe(
    catchError((error) => {
      if (isAuthEndpoint || !(error instanceof HttpErrorResponse) || error.status !== 401) {
        return throwError(() => error);
      }

      const refreshToken = tokenService.getRefreshToken();
      if (!refreshToken) {
        tokenService.clearTokens();
        router.navigate(['/login']);
        return throwError(() => error);
      }

      if (isRefreshing) {
        return refreshToken$.pipe(
          filter(token => token !== null),
          take(1),
          switchMap(token => next(addHeaders(req, token)))
        );
      }

      isRefreshing = true;
      refreshToken$.next(null);

      return authService.refreshToken().pipe(
        tap((response: TokenResponse) => {
          isRefreshing = false;
          refreshToken$.next(response.accessToken);
        }),
        switchMap((response: TokenResponse) => next(addHeaders(req, response.accessToken))),
        catchError((refreshError) => {
          isRefreshing = false;
          refreshToken$.next(null);
          tokenService.clearTokens();
          router.navigate(['/login']);
          return throwError(() => refreshError);
        })
      );
    })
  );
};
