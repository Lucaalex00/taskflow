import { TestBed } from '@angular/core/testing';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { CurrentUserService } from './current-user.service';

describe('CurrentUserService', () => {
  let httpMock: HttpTestingController;

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

  it('starts unregistered when localStorage is empty', () => {
    const service = createService();

    expect(service.isRegistered()).toBeFalse();
    expect(service.userId()).toBeNull();
    expect(service.displayName()).toBeNull();
  });

  it('restores identity already persisted in localStorage', () => {
    localStorage.setItem('taskflow.currentUserId', 'user-1');
    localStorage.setItem('taskflow.currentUserName', 'Ada');

    const service = createService();

    expect(service.isRegistered()).toBeTrue();
    expect(service.userId()).toBe('user-1');
    expect(service.displayName()).toBe('Ada');
  });

  it('register posts the request, persists the identity and updates the signals', async () => {
    const service = createService();

    const promise = service.register({ email: 'ada@example.com', displayName: 'Ada' });

    const req = httpMock.expectOne(`${environment.apiUrl}/users`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ email: 'ada@example.com', displayName: 'Ada' });
    req.flush('user-1');

    expect(await promise).toBe('user-1');
    expect(service.userId()).toBe('user-1');
    expect(service.displayName()).toBe('Ada');
    expect(service.isRegistered()).toBeTrue();
    expect(localStorage.getItem('taskflow.currentUserId')).toBe('user-1');
    expect(localStorage.getItem('taskflow.currentUserName')).toBe('Ada');
  });

  it('signOut clears the identity from signals and localStorage', () => {
    localStorage.setItem('taskflow.currentUserId', 'user-1');
    localStorage.setItem('taskflow.currentUserName', 'Ada');
    const service = createService();

    service.signOut();

    expect(service.isRegistered()).toBeFalse();
    expect(service.userId()).toBeNull();
    expect(service.displayName()).toBeNull();
    expect(localStorage.getItem('taskflow.currentUserId')).toBeNull();
    expect(localStorage.getItem('taskflow.currentUserName')).toBeNull();
  });
});
