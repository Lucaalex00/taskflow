import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./features/boards/board-list/board-list.component').then((m) => m.BoardListComponent)
  },
  {
    path: 'boards/:id',
    loadComponent: () =>
      import('./features/boards/board-detail/board-detail.component').then((m) => m.BoardDetailComponent)
  }
];
