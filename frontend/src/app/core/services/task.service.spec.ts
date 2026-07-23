import { TestBed } from '@angular/core/testing';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { TaskService } from './task.service';
import { TaskDto, TaskPriority, TaskState } from '../models/task.model';

describe('TaskService', () => {
  let service: TaskService;
  let httpMock: HttpTestingController;

  const task: TaskDto = {
    id: 'task-1',
    boardId: 'board-1',
    title: 'Write tests',
    description: null,
    state: TaskState.Todo,
    priority: TaskPriority.Medium,
    assigneeId: null,
    dueAtUtc: null,
    isOverdue: false,
    createdAtUtc: '2026-01-01T00:00:00Z',
    updatedAtUtc: '2026-01-01T00:00:00Z'
  };

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()]
    });
    service = TestBed.inject(TaskService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('getBoardTasks requests tasks for the given board', async () => {
    const promise = service.getBoardTasks('board-1');

    const req = httpMock.expectOne(`${environment.apiUrl}/boards/board-1/tasks`);
    expect(req.request.method).toBe('GET');
    req.flush([task]);

    expect(await promise).toEqual([task]);
  });

  it('create posts the new task request and returns the created id', async () => {
    const request = {
      title: 'Write tests',
      description: null,
      priority: TaskPriority.Medium,
      dueAtUtc: null
    };
    const promise = service.create('board-1', request);

    const req = httpMock.expectOne(`${environment.apiUrl}/boards/board-1/tasks`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(request);
    req.flush('task-1');

    expect(await promise).toBe('task-1');
  });

  it('transitionState patches the task state', async () => {
    const promise = service.transitionState('task-1', TaskState.InProgress);

    const req = httpMock.expectOne(`${environment.apiUrl}/tasks/task-1/state`);
    expect(req.request.method).toBe('PATCH');
    expect(req.request.body).toEqual({ newState: TaskState.InProgress });
    req.flush(null);

    await promise;
  });

  it('assign patches the task assignee', async () => {
    const promise = service.assign('task-1', 'user-1');

    const req = httpMock.expectOne(`${environment.apiUrl}/tasks/task-1/assignee`);
    expect(req.request.method).toBe('PATCH');
    expect(req.request.body).toEqual({ userId: 'user-1' });
    req.flush(null);

    await promise;
  });
});
