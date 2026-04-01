import { Injectable } from '@angular/core';

@Injectable({
  providedIn: 'root'
})
export class TokenService {
  private readonly ACCESS_TOKEN_KEY = 'access_token';
  private readonly REFRESH_TOKEN_KEY = 'refresh_token';

  saveTokens(accessToken: string, refreshToken: string): void {
    // Strip any accidental surrounding quotes (e.g. from JSON.stringify)
    const cleanAccess = accessToken?.replace(/^"|"$/g, '').trim();
    const cleanRefresh = refreshToken?.replace(/^"|"$/g, '').trim();
    console.log('[TokenService] Saving access token (first 30):', cleanAccess?.substring(0, 30));
    localStorage.setItem(this.ACCESS_TOKEN_KEY, cleanAccess);
    localStorage.setItem(this.REFRESH_TOKEN_KEY, cleanRefresh);
  }

  saveAccessToken(accessToken: string): void {
    localStorage.setItem(this.ACCESS_TOKEN_KEY, accessToken);
  }

  getAccessToken(): string | null {
    const raw = localStorage.getItem(this.ACCESS_TOKEN_KEY);
    return raw ? raw.replace(/^"|"$/g, '').trim() : null;
  }

  getRefreshToken(): string | null {
    const raw = localStorage.getItem(this.REFRESH_TOKEN_KEY);
    return raw ? raw.replace(/^"|"$/g, '').trim() : null;
  }

  clearTokens(): void {
    localStorage.removeItem(this.ACCESS_TOKEN_KEY);
    localStorage.removeItem(this.REFRESH_TOKEN_KEY);
  }

  hasValidToken(): boolean {
    const token = this.getAccessToken();
    if (!token) return false;

    try {
      const decoded = this.decodeToken(token);
      return decoded.exp > Date.now() / 1000;
    } catch {
      return false;
    }
  }

  decodeToken(token: string): any {
    const payload = token.split('.')[1];
    return JSON.parse(atob(payload));
  }
}
