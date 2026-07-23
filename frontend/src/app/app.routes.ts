import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';

export const routes: Routes = [
  {
    path: 'login',
    loadComponent: () => import('./features/auth/login/login.component').then((m) => m.LoginComponent)
  },
  {
    path: '',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./features/boards/board-list/board-list.component').then((m) => m.BoardListComponent)
  },
  {
    path: 'boards/:id',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./features/boards/board-detail/board-detail.component').then((m) => m.BoardDetailComponent)
  }
];
