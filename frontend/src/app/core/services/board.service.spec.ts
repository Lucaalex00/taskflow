import { TestBed } from '@angular/core/testing';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { BoardService } from './board.service';
import { BoardDto, BoardMemberDto, BoardRole } from '../models/board.model';

describe('BoardService', () => {
  let service: BoardService;
  let httpMock: HttpTestingController;

  const board: BoardDto = {
    id: 'board-1',
    name: 'Sprint 1',
    ownerId: 'user-1',
    color: '#4fd1c5',
    taskCount: 3,
    createdAtUtc: '2026-01-01T00:00:00Z'
  };

  const member: BoardMemberDto = {
    userId: 'user-1',
    displayName: 'Ada',
    email: 'ada@example.com',
    color: '#4fd1c5',
    role: BoardRole.Owner
  };

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()]
    });
    service = TestBed.inject(BoardService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('getAll requests the boards list', async () => {
    const promise = service.getAll();

    const req = httpMock.expectOne(`${environment.apiUrl}/boards`);
    expect(req.request.method).toBe('GET');
    req.flush([board]);

    expect(await promise).toEqual([board]);
  });

  it('create posts the new board request and returns the created id', async () => {
    const promise = service.create({ name: 'Sprint 1' });

    const req = httpMock.expectOne(`${environment.apiUrl}/boards`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ name: 'Sprint 1' });
    req.flush('board-1');

    expect(await promise).toBe('board-1');
  });

  it('getMembers requests the board members list', async () => {
    const promise = service.getMembers('board-1');

    const req = httpMock.expectOne(`${environment.apiUrl}/boards/board-1/members`);
    expect(req.request.method).toBe('GET');
    req.flush([member]);

    expect(await promise).toEqual([member]);
  });

  it('addMember posts the new member request', async () => {
    const promise = service.addMember('board-1', { userId: 'user-2', role: BoardRole.Member });

    const req = httpMock.expectOne(`${environment.apiUrl}/boards/board-1/members`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ userId: 'user-2', role: BoardRole.Member });
    req.flush(null);

    await promise;
  });

  it('removeMember deletes the member', async () => {
    const promise = service.removeMember('board-1', 'user-2');

    const req = httpMock.expectOne(`${environment.apiUrl}/boards/board-1/members/user-2`);
    expect(req.request.method).toBe('DELETE');
    req.flush(null);

    await promise;
  });
});
