import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { CurrentUserService } from '../services/current-user.service';

/** Redirects to /login unless the user is signed in. */
export const authGuard: CanActivateFn = () => {
  const currentUser = inject(CurrentUserService);
  const router = inject(Router);

  if (currentUser.isAuthenticated()) {
    return true;
  }

  return router.parseUrl('/login');
};
