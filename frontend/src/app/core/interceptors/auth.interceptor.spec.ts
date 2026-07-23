import { TestBed } from '@angular/core/testing';
import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter, Router } from '@angular/router';
import { CurrentUserService } from '../services/current-user.service';
import { authInterceptor } from './auth.interceptor';

describe('authInterceptor', () => {
  let router: Router;

  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([authInterceptor])),
        provideHttpClientTesting(),
        provideRouter([])
      ]
    });
  });

  afterEach(() => {
    TestBed.inject(HttpTestingController).verify();
    localStorage.clear();
  });

  it('attaches the Authorization header when a token is present', () => {
    localStorage.setItem('taskflow.authToken', 'jwt-token');
    const httpClient = TestBed.inject(HttpClient);
    const httpMock = TestBed.inject(HttpTestingController);

    httpClient.get('/api/boards').subscribe();

    const req = httpMock.expectOne('/api/boards');
    expect(req.request.headers.get('Authorization')).toBe('Bearer jwt-token');
    req.flush([]);
  });

  it('does not attach an Authorization header when there is no token', () => {
    const httpClient = TestBed.inject(HttpClient);
    const httpMock = TestBed.inject(HttpTestingController);

    httpClient.get('/api/boards').subscribe();

    const req = httpMock.expectOne('/api/boards');
    expect(req.request.headers.has('Authorization')).toBeFalse();
    req.flush([]);
  });

  it('signs out and redirects to /login on a 401 response', () => {
    localStorage.setItem('taskflow.authToken', 'jwt-token');
    const currentUser = TestBed.inject(CurrentUserService);
    const httpClient = TestBed.inject(HttpClient);
    const httpMock = TestBed.inject(HttpTestingController);
    router = TestBed.inject(Router);
    spyOn(router, 'navigateByUrl');

    httpClient.get('/api/boards').subscribe({ error: () => undefined });

    const req = httpMock.expectOne('/api/boards');
    req.flush({ message: 'expired' }, { status: 401, statusText: 'Unauthorized' });

    expect(currentUser.isAuthenticated()).toBeFalse();
    expect(router.navigateByUrl).toHaveBeenCalledWith('/login');
  });
});
