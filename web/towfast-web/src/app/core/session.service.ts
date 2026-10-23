import { Injectable, computed, signal } from '@angular/core';
import { UserResponse } from '../api/generated/models/user-response';
import { AuthResponse } from '../api/generated/models/auth-response';

const REFRESH_KEY = 'towfast.refresh-token';
const USER_KEY = 'towfast.user';

@Injectable({ providedIn: 'root' })
export class SessionService {
  private readonly accessState = signal<string | null>(null);
  private readonly userState = signal<UserResponse | null>(this.readUser());
  readonly accessToken = this.accessState.asReadonly();
  readonly user = this.userState.asReadonly();
  readonly authenticated = computed(() => !!this.accessState() && !!this.userState());
  readonly role = computed(() => this.userState()?.role ?? null);
  get refreshToken(): string | null { return sessionStorage.getItem(REFRESH_KEY); }

  set(auth: AuthResponse): void {
    if (auth.refreshToken) sessionStorage.setItem(REFRESH_KEY, auth.refreshToken);
    if (auth.user) sessionStorage.setItem(USER_KEY, JSON.stringify(auth.user));
    this.accessState.set(auth.accessToken ?? null);
    this.userState.set(auth.user ?? null);
  }
  clear(): void {
    sessionStorage.removeItem(REFRESH_KEY); sessionStorage.removeItem(USER_KEY);
    this.accessState.set(null); this.userState.set(null);
  }
  private readUser(): UserResponse | null { try { return JSON.parse(sessionStorage.getItem(USER_KEY) ?? 'null'); } catch { return null; } }
}
