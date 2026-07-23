import { TestBed } from '@angular/core/testing';
import { Router, UrlTree } from '@angular/router';
import { CurrentUserService } from '../services/current-user.service';
import { authGuard } from './auth.guard';

describe('authGuard', () => {
  let currentUser: jasmine.SpyObj<CurrentUserService>;
  let router: jasmine.SpyObj<Router>;
  const urlTree = {} as UrlTree;

  beforeEach(() => {
    currentUser = jasmine.createSpyObj<CurrentUserService>('CurrentUserService', ['isAuthenticated']);
    router = jasmine.createSpyObj<Router>('Router', ['parseUrl']);
    router.parseUrl.and.returnValue(urlTree);

    TestBed.configureTestingModule({
      providers: [
        { provide: CurrentUserService, useValue: currentUser },
        { provide: Router, useValue: router }
      ]
    });
  });

  function runGuard() {
    return TestBed.runInInjectionContext(() => authGuard({} as never, {} as never));
  }

  it('allows navigation when the user is authenticated', () => {
    currentUser.isAuthenticated.and.returnValue(true);

    expect(runGuard()).toBeTrue();
  });

  it('redirects to /login when the user is not authenticated', () => {
    currentUser.isAuthenticated.and.returnValue(false);

    expect(runGuard()).toBe(urlTree);
    expect(router.parseUrl).toHaveBeenCalledWith('/login');
  });
});
