import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap } from '@angular/router';
import { signal } from '@angular/core';
import { BoardDetailComponent } from './board-detail.component';
import { TaskService } from '../../../core/services/task.service';
import { AlertService } from '../../../core/services/alert.service';
import { CurrentUserService } from '../../../core/services/current-user.service';
import { TaskDto, TaskPriority, TaskState } from '../../../core/models/task.model';
import { AlertDto, AlertSeverity } from '../../../core/models/alert.model';

describe('BoardDetailComponent', () => {
  let taskService: jasmine.SpyObj<TaskService>;
  let alertService: {
    alerts: ReturnType<typeof signal<AlertDto[]>>;
    connectToBoard: jasmine.Spy;
    disconnect: jasmine.Spy;
    markRead: jasmine.Spy;
  };
  let currentUser: jasmine.SpyObj<CurrentUserService>;

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

  const alert: AlertDto = {
    id: 'alert-1',
    boardId: 'board-1',
    severity: AlertSeverity.Warning,
    message: 'Too many tasks in progress',
    relatedUserId: 'user-1',
    isRead: false,
    createdAtUtc: '2026-01-01T00:00:00Z'
  };

  function createComponent() {
    taskService = jasmine.createSpyObj<TaskService>('TaskService', [
      'getBoardTasks',
      'create',
      'transitionState',
      'assign'
    ]);
    alertService = {
      alerts: signal<AlertDto[]>([]),
      connectToBoard: jasmine.createSpy('connectToBoard').and.resolveTo(undefined),
      disconnect: jasmine.createSpy('disconnect').and.resolveTo(undefined),
      markRead: jasmine.createSpy('markRead')
    };

    currentUser = jasmine.createSpyObj<CurrentUserService>('CurrentUserService', ['userId']);
    currentUser.userId.and.returnValue('user-1');

    TestBed.configureTestingModule({
      imports: [BoardDetailComponent],
      providers: [
        { provide: TaskService, useValue: taskService },
        { provide: AlertService, useValue: alertService },
        { provide: CurrentUserService, useValue: currentUser },
        {
          provide: ActivatedRoute,
          useValue: { snapshot: { paramMap: convertToParamMap({ id: 'board-1' }) } }
        }
      ]
    });

    const fixture = TestBed.createComponent(BoardDetailComponent);
    return { fixture, component: fixture.componentInstance };
  }

  it('reads the board id from the route', () => {
    const { component } = createComponent();

    expect(component.boardId).toBe('board-1');
  });

  it('loads tasks and connects to the alerts hub on init', async () => {
    const { component } = createComponent();
    taskService.getBoardTasks.and.resolveTo([task]);

    await component.ngOnInit();

    expect(taskService.getBoardTasks).toHaveBeenCalledWith('board-1');
    expect(alertService.connectToBoard).toHaveBeenCalledWith('board-1');
    expect(component.tasks()).toEqual([task]);
    expect(component.isLoading()).toBeFalse();
  });

  it('sets an error message when loading tasks fails', async () => {
    const { component } = createComponent();
    taskService.getBoardTasks.and.rejectWith(new Error('boom'));

    await component.ngOnInit();

    expect(component.errorMessage()).toBe('Could not load tasks for this board.');
  });

  it('groups tasks by column, keeping cancelled tasks out of the board columns', async () => {
    const doneTask: TaskDto = { ...task, id: 'task-2', state: TaskState.Done };
    const cancelledTask: TaskDto = { ...task, id: 'task-3', state: TaskState.Cancelled };
    const { component } = createComponent();
    taskService.getBoardTasks.and.resolveTo([task, doneTask, cancelledTask]);

    await component.ngOnInit();

    expect(component.tasksByColumn().get(TaskState.Todo)).toEqual([task]);
    expect(component.tasksByColumn().get(TaskState.Done)).toEqual([doneTask]);
    expect(component.cancelledCount()).toBe(1);
  });

  it('transitionsFor mirrors the backend state machine', () => {
    const { component } = createComponent();

    expect(component.transitionsFor(task)).toEqual([TaskState.InProgress, TaskState.Cancelled]);
    expect(component.transitionsFor({ ...task, state: TaskState.Done })).toEqual([]);
  });

  it('moveTask transitions the task and reloads the board', async () => {
    const { component } = createComponent();
    taskService.transitionState.and.resolveTo(undefined);
    taskService.getBoardTasks.and.resolveTo([]);

    await component.moveTask(task, TaskState.InProgress);

    expect(taskService.transitionState).toHaveBeenCalledWith('task-1', TaskState.InProgress);
    expect(taskService.getBoardTasks).toHaveBeenCalled();
  });

  it('moveTask sets an error message when the transition fails', async () => {
    const { component } = createComponent();
    taskService.transitionState.and.rejectWith(new Error('boom'));

    await component.moveTask(task, TaskState.InProgress);

    expect(component.errorMessage()).toContain('Could not move');
  });

  it('assignToSelf assigns the task to the current user', async () => {
    const { component } = createComponent();
    taskService.assign.and.resolveTo(undefined);
    taskService.getBoardTasks.and.resolveTo([]);

    await component.assignToSelf(task);

    expect(taskService.assign).toHaveBeenCalledWith('task-1', 'user-1');
  });

  it('createTask creates the task, resets the form and reloads the board', async () => {
    const { component } = createComponent();
    taskService.create.and.resolveTo('task-2');
    taskService.getBoardTasks.and.resolveTo([]);
    component.newTaskTitle = 'Write tests';
    component.newTaskDescription = 'For the board detail component';
    component.isTaskFormOpen.set(true);

    await component.createTask();

    expect(taskService.create).toHaveBeenCalledWith('board-1', {
      title: 'Write tests',
      description: 'For the board detail component',
      priority: TaskPriority.Medium,
      dueAtUtc: null
    });
    expect(component.newTaskTitle).toBe('');
    expect(component.isTaskFormOpen()).toBeFalse();
  });

  it('createTask does nothing when the title is blank', async () => {
    const { component } = createComponent();
    component.newTaskTitle = '   ';

    await component.createTask();

    expect(taskService.create).not.toHaveBeenCalled();
  });

  it('markAlertRead marks the alert read remotely and updates the local signal', async () => {
    const { component } = createComponent();
    alertService.markRead.and.resolveTo(undefined);
    alertService.alerts.set([alert]);

    await component.markAlertRead('alert-1');

    expect(alertService.markRead).toHaveBeenCalledWith('alert-1');
    expect(alertService.alerts()[0].isRead).toBeTrue();
  });

  it('ngOnDestroy disconnects the alerts hub', async () => {
    const { component } = createComponent();

    await component.ngOnDestroy();

    expect(alertService.disconnect).toHaveBeenCalled();
  });
});
