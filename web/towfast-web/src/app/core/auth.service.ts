import { Injectable, inject } from '@angular/core';
import { Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { TowFastApiService } from './towfast-api.service';
import { SessionService } from './session.service';
import { LoginCommand } from '../api/generated/models/login-command';
import { RegisterClientCommand } from '../api/generated/models/register-client-command';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly api = inject(TowFastApiService);
  private readonly session = inject(SessionService);
  private readonly router = inject(Router);
  private refreshPromise?: Promise<string | null>;
  async login(command: LoginCommand): Promise<void> { this.session.set(await firstValueFrom(this.api.login(command))); }
  async register(command: RegisterClientCommand): Promise<void> { this.session.set(await firstValueFrom(this.api.register(command))); }
  refresh(): Promise<string | null> {
    if (this.refreshPromise) return this.refreshPromise;
    const token = this.session.refreshToken;
    if (!token) return Promise.resolve(null);
    this.refreshPromise = firstValueFrom(this.api.refresh(token)).then(auth => { this.session.set(auth); return auth.accessToken ?? null; }).catch(() => { this.session.clear(); return null; }).finally(() => { this.refreshPromise = undefined; });
    return this.refreshPromise;
  }
  async logout(): Promise<void> { try { await firstValueFrom(this.api.logout(this.session.refreshToken)); } finally { this.session.clear(); await this.router.navigateByUrl('/entrar'); } }
}
