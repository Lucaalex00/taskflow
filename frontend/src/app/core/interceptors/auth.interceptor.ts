import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';
import { CurrentUserService } from '../services/current-user.service';

/**
 * Attaches the signed-in user's JWT to every outgoing request, and signs them out (redirecting
 * to /login) if the API ever rejects it as expired/invalid.
 */
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const currentUser = inject(CurrentUserService);
  const router = inject(Router);
  const token = currentUser.token();

  const request = token ? req.clone({ setHeaders: { Authorization: `Bearer ${token}` } }) : req;

  return next(request).pipe(
    catchError((error: unknown) => {
      if (error instanceof HttpErrorResponse && error.status === 401) {
        currentUser.signOut();
        router.navigateByUrl('/login');
      }
      return throwError(() => error);
    })
  );
};
