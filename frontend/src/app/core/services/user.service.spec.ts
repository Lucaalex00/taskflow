import { TestBed } from '@angular/core/testing';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { UserService } from './user.service';
import { UserDto } from '../models/user.model';

describe('UserService', () => {
  let service: UserService;
  let httpMock: HttpTestingController;

  const user: UserDto = {
    id: 'user-1',
    displayName: 'Ada',
    email: 'ada@example.com'
  };

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()]
    });
    service = TestBed.inject(UserService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('getAll requests every registered user', async () => {
    const promise = service.getAll();

    const req = httpMock.expectOne(`${environment.apiUrl}/users`);
    expect(req.request.method).toBe('GET');
    req.flush([user]);

    expect(await promise).toEqual([user]);
  });
});
