import { Routes } from '@angular/router';
import { authGuard, guestGuard, roleGuard } from './core/guards';

export const routes: Routes = [
  {
    path: 'entrar',
    canActivate: [guestGuard],
    loadComponent: () => import('./features/auth/auth-page').then((m) => m.AuthPage),
  },
  {
    path: 'cadastro',
    canActivate: [guestGuard],
    loadComponent: () => import('./features/auth/auth-page').then((m) => m.AuthPage),
  },
  {
    path: 'app',
    canActivate: [authGuard],
    loadComponent: () => import('./layout/app-shell').then((m) => m.AppShell),
    children: [
      {
        path: '',
        loadComponent: () =>
          import('./features/overview/overview-page').then((m) => m.OverviewPage),
      },
      {
        path: 'atendimentos',
        loadComponent: () =>
          import('./features/requests/requests-page').then((m) => m.RequestsPage),
      },
      {
        path: 'minha-conta',
        canActivate: [roleGuard],
        data: { roles: ['Client', 'Admin', 'Operator'] },
        loadComponent: () => import('./features/account/account-page').then((m) => m.AccountPage),
      },
      {
        path: 'equipe',
        canActivate: [roleGuard],
        data: { roles: ['Provider', 'Admin', 'Operator'] },
        loadComponent: () => import('./features/team/team-page').then((m) => m.TeamPage),
      },
      {
        path: 'gestao',
        canActivate: [roleGuard],
        data: { roles: ['Admin', 'Operator'] },
        loadComponent: () => import('./features/admin/admin-page').then((m) => m.AdminPage),
      },
    ],
  },
  { path: '', pathMatch: 'full', redirectTo: 'entrar' },
  { path: '**', redirectTo: 'app' },
];
