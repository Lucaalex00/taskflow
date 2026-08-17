import { TestBed } from '@angular/core/testing';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { CurrentUserService } from './current-user.service';
import { AuthResult } from '../models/user.model';

describe('CurrentUserService', () => {
  let httpMock: HttpTestingController;

  const authResult: AuthResult = {
    userId: 'user-1',
    displayName: 'Ada',
    color: '#4fd1c5',
    token: 'jwt-token'
  };

  function createService(): CurrentUserService {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()]
    });
    httpMock = TestBed.inject(HttpTestingController);
    return TestBed.inject(CurrentUserService);
  }

  beforeEach(() => {
    localStorage.clear();
  });

  afterEach(() => {
    httpMock.verify();
    localStorage.clear();
  });

  it('starts unauthenticated when localStorage is empty', () => {
    const service = createService();

    expect(service.isAuthenticated()).toBeFalse();
    expect(service.token()).toBeNull();
    expect(service.userId()).toBeNull();
    expect(service.displayName()).toBeNull();
    expect(service.color()).toBeNull();
  });

  it('restores identity already persisted in localStorage', () => {
    localStorage.setItem('taskflow.authToken', 'jwt-token');
    localStorage.setItem('taskflow.currentUserId', 'user-1');
    localStorage.setItem('taskflow.currentUserName', 'Ada');
    localStorage.setItem('taskflow.currentUserColor', '#4fd1c5');

    const service = createService();

    expect(service.isAuthenticated()).toBeTrue();
    expect(service.token()).toBe('jwt-token');
    expect(service.userId()).toBe('user-1');
    expect(service.displayName()).toBe('Ada');
    expect(service.color()).toBe('#4fd1c5');
  });

  it('register posts the request, persists the token and identity and updates the signals', async () => {
    const service = createService();

    const promise = service.register({ email: 'ada@example.com', displayName: 'Ada', password: 'password123' });

    const req = httpMock.expectOne(`${environment.apiUrl}/users`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ email: 'ada@example.com', displayName: 'Ada', password: 'password123' });
    req.flush(authResult);

    await promise;
    expect(service.token()).toBe('jwt-token');
    expect(service.userId()).toBe('user-1');
    expect(service.displayName()).toBe('Ada');
    expect(service.color()).toBe('#4fd1c5');
    expect(service.isAuthenticated()).toBeTrue();
    expect(localStorage.getItem('taskflow.authToken')).toBe('jwt-token');
    expect(localStorage.getItem('taskflow.currentUserId')).toBe('user-1');
    expect(localStorage.getItem('taskflow.currentUserName')).toBe('Ada');
    expect(localStorage.getItem('taskflow.currentUserColor')).toBe('#4fd1c5');
  });

  it('login posts the request and updates the signals the same way register does', async () => {
    const service = createService();

    const promise = service.login({ email: 'ada@example.com', password: 'password123' });

    const req = httpMock.expectOne(`${environment.apiUrl}/auth/login`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ email: 'ada@example.com', password: 'password123' });
    req.flush(authResult);

    await promise;
    expect(service.isAuthenticated()).toBeTrue();
    expect(service.userId()).toBe('user-1');
  });

  it('signOut clears the identity and token from signals and localStorage', () => {
    localStorage.setItem('taskflow.authToken', 'jwt-token');
    localStorage.setItem('taskflow.currentUserId', 'user-1');
    localStorage.setItem('taskflow.currentUserName', 'Ada');
    localStorage.setItem('taskflow.currentUserColor', '#4fd1c5');
    const service = createService();

    service.signOut();

    expect(service.isAuthenticated()).toBeFalse();
    expect(service.token()).toBeNull();
    expect(service.userId()).toBeNull();
    expect(service.displayName()).toBeNull();
    expect(service.color()).toBeNull();
    expect(localStorage.getItem('taskflow.authToken')).toBeNull();
    expect(localStorage.getItem('taskflow.currentUserId')).toBeNull();
    expect(localStorage.getItem('taskflow.currentUserName')).toBeNull();
    expect(localStorage.getItem('taskflow.currentUserColor')).toBeNull();
  });

  it('updateDisplayName patches the profile and keeps the cached name in step', async () => {
    localStorage.setItem('taskflow.currentUserName', 'Ada');
    const service = createService();

    const promise = service.updateDisplayName('Ada Lovelace');

    const req = httpMock.expectOne(`${environment.apiUrl}/users/me`);
    expect(req.request.method).toBe('PATCH');
    expect(req.request.body).toEqual({ displayName: 'Ada Lovelace' });
    req.flush({ id: 'user-1', displayName: 'Ada Lovelace', email: 'ada@example.com', color: '#4fd1c5' });

    await promise;
    expect(service.displayName()).toBe('Ada Lovelace');
    expect(localStorage.getItem('taskflow.currentUserName')).toBe('Ada Lovelace');
  });

  it('changePassword posts both passwords and leaves the session token untouched', async () => {
    localStorage.setItem('taskflow.authToken', 'jwt-token');
    const service = createService();

    const promise = service.changePassword('Old-password-1', 'New-password-2');

    const req = httpMock.expectOne(`${environment.apiUrl}/users/me/password`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ currentPassword: 'Old-password-1', newPassword: 'New-password-2' });
    req.flush(null);

    await promise;
    expect(service.token()).toBe('jwt-token');
    expect(service.isAuthenticated()).toBeTrue();
  });
});
