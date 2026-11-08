import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { NzMessageService } from 'ng-zorro-antd/message';
import { catchError, from, switchMap, throwError } from 'rxjs';
import { SessionService } from './session.service';
import { AuthService } from './auth.service';
import { SILENT_HTTP_ERROR } from './http-context-tokens';

function createCorrelationId(): string {
  if (typeof crypto.randomUUID === 'function') return crypto.randomUUID();
  const bytes = crypto.getRandomValues(new Uint8Array(16));
  bytes[6] = (bytes[6] & 0x0f) | 0x40;
  bytes[8] = (bytes[8] & 0x3f) | 0x80;
  const hex = Array.from(bytes, value => value.toString(16).padStart(2, '0')).join('');
  return `${hex.slice(0, 8)}-${hex.slice(8, 12)}-${hex.slice(12, 16)}-${hex.slice(16, 20)}-${hex.slice(20)}`;
}

export const correlationInterceptor: HttpInterceptorFn = (req, next) =>
  next(req.clone({ setHeaders: { 'X-Correlation-ID': createCorrelationId() } }));

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const session = inject(SessionService); const auth = inject(AuthService); const router = inject(Router);
  const isAuth = ['/auth/login', '/auth/register', '/auth/refresh'].some(path => req.url.includes(path));
  const send = (token: string | null) => next(token && !isAuth ? req.clone({ setHeaders: { Authorization: `Bearer ${token}` } }) : req);
  return send(session.accessToken()).pipe(catchError(error => {
    if (!(error instanceof HttpErrorResponse) || error.status !== 401 || isAuth) return throwError(() => error);
    return from(auth.refresh()).pipe(switchMap(token => {
      if (!token) { void router.navigateByUrl('/entrar'); return throwError(() => error); }
      return send(token);
    }));
  }));
};

export const problemInterceptor: HttpInterceptorFn = (req, next) => {
  const messages = inject(NzMessageService);
  return next(req).pipe(catchError((error: HttpErrorResponse) => {
    const correlationId = error.error?.correlationId ?? error.headers.get('X-Correlation-ID');
    const base = error.error?.detail ?? error.error?.title ?? (error.status === 0 ? 'Não foi possível conectar à API.' : 'Não foi possível concluir a operação.');
    const detail = correlationId ? `${base} Referência: ${correlationId}` : base;
    if (error.status !== 401 && !req.context.get(SILENT_HTTP_ERROR)) {
      const title = error.status === 409 ? 'Os dados mudaram' : 'Algo não saiu como esperado';
      messages.create(error.status === 409 ? 'warning' : 'error', `${title}: ${detail}`, { nzDuration: 7000 });
    }
    return throwError(() => error);
  }));
};
