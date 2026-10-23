import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { SessionService } from './session.service';
import { UserRole } from '../api/generated/models/user-role';
import { AuthService } from './auth.service';

export const authGuard: CanActivateFn = async () => {
  const session=inject(SessionService);const router=inject(Router);if(session.authenticated())return true;
  return session.refreshToken && await inject(AuthService).refresh() ? true : router.createUrlTree(['/entrar']);
};
export const guestGuard: CanActivateFn = async () => {
  const session=inject(SessionService);const router=inject(Router);if(session.authenticated())return router.createUrlTree(['/app']);
  return session.refreshToken && await inject(AuthService).refresh() ? router.createUrlTree(['/app']) : true;
};
export const roleGuard: CanActivateFn = route => (route.data['roles'] as UserRole[]).includes(inject(SessionService).role()!) || inject(Router).createUrlTree(['/app']);
