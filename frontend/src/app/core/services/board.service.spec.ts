import { TestBed } from '@angular/core/testing';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { BoardService } from './board.service';
import { BoardDto } from '../models/board.model';

describe('BoardService', () => {
  let service: BoardService;
  let httpMock: HttpTestingController;

  const board: BoardDto = {
    id: 'board-1',
    name: 'Sprint 1',
    ownerId: 'user-1',
    taskCount: 3,
    createdAtUtc: '2026-01-01T00:00:00Z'
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
    const promise = service.create({ name: 'Sprint 1', ownerId: 'user-1' });

    const req = httpMock.expectOne(`${environment.apiUrl}/boards`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ name: 'Sprint 1', ownerId: 'user-1' });
    req.flush('board-1');

    expect(await promise).toBe('board-1');
  });
});
