import { TestBed, fakeAsync, tick } from '@angular/core/testing';
import { HttpErrorResponse } from '@angular/common/http';
import { ActivatedRoute, convertToParamMap } from '@angular/router';
import { signal } from '@angular/core';
import { BoardDetailComponent } from './board-detail.component';
import { TaskService } from '../../../core/services/task.service';
import { AlertService } from '../../../core/services/alert.service';
import { CurrentUserService } from '../../../core/services/current-user.service';
import { BoardService } from '../../../core/services/board.service';
import { ToastService } from '../../../core/services/toast.service';
import { TaskDto, TaskPriority, TaskState } from '../../../core/models/task.model';
import { AlertDto, AlertSeverity } from '../../../core/models/alert.model';
import { BoardDto, BoardMemberDto, BoardRole } from '../../../core/models/board.model';

describe('BoardDetailComponent', () => {
  let taskService: jasmine.SpyObj<TaskService>;
  let boardService: jasmine.SpyObj<BoardService>;
  let alertService: {
    alerts: ReturnType<typeof signal<AlertDto[]>>;
    connectToBoard: jasmine.Spy;
    disconnect: jasmine.Spy;
    markRead: jasmine.Spy;
  };
  let currentUser: jasmine.SpyObj<CurrentUserService>;
  let toast: jasmine.SpyObj<ToastService>;

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
    isArchived: false,
    createdAtUtc: '2026-01-01T00:00:00Z',
    updatedAtUtc: '2026-01-01T00:00:00Z'
  };

  const owner: BoardMemberDto = {
    userId: 'user-1',
    displayName: 'Ada',
    email: 'ada@example.com',
    color: '#4fd1c5',
    role: BoardRole.Owner
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
      'update',
      'transitionState',
      'assign',
      'archive'
    ]);
    boardService = jasmine.createSpyObj<BoardService>(
      'BoardService',
      ['getAll', 'create', 'refresh', 'getMembers', 'inviteMember', 'updateMemberRole', 'removeMember'],
      { boards: signal<BoardDto[]>([]) }
    );
    boardService.refresh.and.resolveTo(undefined);
    boardService.getMembers.and.resolveTo([]);
    alertService = {
      alerts: signal<AlertDto[]>([]),
      connectToBoard: jasmine.createSpy('connectToBoard').and.resolveTo(undefined),
      disconnect: jasmine.createSpy('disconnect').and.resolveTo(undefined),
      markRead: jasmine.createSpy('markRead')
    };

    currentUser = jasmine.createSpyObj<CurrentUserService>('CurrentUserService', ['userId']);
    currentUser.userId.and.returnValue('user-1');

    toast = jasmine.createSpyObj<ToastService>('ToastService', ['success', 'error', 'info']);

    TestBed.configureTestingModule({
      imports: [BoardDetailComponent],
      providers: [
        { provide: TaskService, useValue: taskService },
        { provide: BoardService, useValue: boardService },
        { provide: AlertService, useValue: alertService },
        { provide: CurrentUserService, useValue: currentUser },
        { provide: ToastService, useValue: toast },
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

  it('loads tasks, members and connects to the alerts hub on init', async () => {
    const { component } = createComponent();
    taskService.getBoardTasks.and.resolveTo([task]);
    boardService.getMembers.and.resolveTo([owner]);

    await component.ngOnInit();

    expect(taskService.getBoardTasks).toHaveBeenCalledWith('board-1', false);
    expect(boardService.getMembers).toHaveBeenCalledWith('board-1');
    expect(alertService.connectToBoard).toHaveBeenCalledWith('board-1');
    expect(component.tasks()).toEqual([task]);
    expect(component.members()).toEqual([owner]);
    expect(component.isLoading()).toBeFalse();
  });

  it('silently ignores a failure to load members, keeping the assignee list empty', async () => {
    const { component } = createComponent();
    taskService.getBoardTasks.and.resolveTo([]);
    boardService.getMembers.and.rejectWith(new Error('boom'));

    await component.ngOnInit();

    expect(component.members()).toEqual([]);
    expect(component.errorMessage()).toBeNull();
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

  it('connectedDropListsFor only offers columns a card may legally be dragged into', () => {
    const { component } = createComponent();

    // Todo can only move to In progress (Cancelled has no column) — never straight to Done/Blocked.
    expect(component.connectedDropListsFor(TaskState.Todo)).toEqual(['column-InProgress']);

    // In progress can move to Blocked, Done, or back to Todo.
    expect(component.connectedDropListsFor(TaskState.InProgress)).toEqual([
      'column-Todo',
      'column-Blocked',
      'column-Done'
    ]);

    // Done is terminal — no drop targets.
    expect(component.connectedDropListsFor(TaskState.Done)).toEqual([]);
  });

  it('filters tasks by search text (title or description)', async () => {
    const other: TaskDto = { ...task, id: 'task-2', title: 'Deploy to prod', description: 'infra' };
    const { component } = createComponent();
    taskService.getBoardTasks.and.resolveTo([task, other]);
    await component.ngOnInit();

    component.searchText.set('deploy');
    expect(component.tasksByColumn().get(TaskState.Todo)).toEqual([other]);

    component.searchText.set('infra'); // matches the description
    expect(component.tasksByColumn().get(TaskState.Todo)).toEqual([other]);

    component.searchText.set('nothing-matches');
    expect(component.tasksByColumn().get(TaskState.Todo)).toEqual([]);
  });

  it('filters tasks by assignee, including an "unassigned" option', async () => {
    const assigned: TaskDto = { ...task, id: 'task-2', assigneeId: 'user-9' };
    const { component } = createComponent();
    taskService.getBoardTasks.and.resolveTo([task, assigned]); // task is unassigned
    await component.ngOnInit();

    component.assigneeFilter.set('user-9');
    expect(component.tasksByColumn().get(TaskState.Todo)).toEqual([assigned]);

    component.assigneeFilter.set('unassigned');
    expect(component.tasksByColumn().get(TaskState.Todo)).toEqual([task]);
  });

  it('filters tasks by priority', async () => {
    const high: TaskDto = { ...task, id: 'task-2', priority: TaskPriority.High };
    const { component } = createComponent();
    taskService.getBoardTasks.and.resolveTo([task, high]); // task is Medium
    await component.ngOnInit();

    component.priorityFilter.set(TaskPriority.High);
    expect(component.tasksByColumn().get(TaskState.Todo)).toEqual([high]);
  });

  it('clearFilters resets every filter and hasActiveFilters', async () => {
    const { component } = createComponent();
    taskService.getBoardTasks.and.resolveTo([task]);
    await component.ngOnInit();

    component.searchText.set('x');
    component.assigneeFilter.set('unassigned');
    component.priorityFilter.set(TaskPriority.High);
    expect(component.hasActiveFilters()).toBeTrue();

    component.clearFilters();
    expect(component.hasActiveFilters()).toBeFalse();
    expect(component.tasksByColumn().get(TaskState.Todo)).toEqual([task]);
  });

  it('moveTask transitions the task and reloads the board', async () => {
    const { component } = createComponent();
    taskService.transitionState.and.resolveTo(undefined);
    taskService.getBoardTasks.and.resolveTo([]);

    await component.moveTask(task, TaskState.InProgress);

    expect(taskService.transitionState).toHaveBeenCalledWith('task-1', TaskState.InProgress);
    expect(taskService.getBoardTasks).toHaveBeenCalled();
  });

  it('requestMove to Done asks for confirmation instead of moving immediately', async () => {
    const { component } = createComponent();

    await component.requestMove(task, TaskState.Done);

    expect(component.pendingDoneTask()).toEqual(task);
    expect(taskService.transitionState).not.toHaveBeenCalled();
  });

  it('requestMove to a non-Done state moves immediately (no confirmation)', async () => {
    const { component } = createComponent();
    taskService.transitionState.and.resolveTo(undefined);
    taskService.getBoardTasks.and.resolveTo([]);

    await component.requestMove(task, TaskState.InProgress);

    expect(component.pendingDoneTask()).toBeNull();
    expect(taskService.transitionState).toHaveBeenCalledWith('task-1', TaskState.InProgress);
  });

  it('confirmMoveToDone moves the pending task to Done and clears the prompt', async () => {
    const { component } = createComponent();
    taskService.transitionState.and.resolveTo(undefined);
    taskService.getBoardTasks.and.resolveTo([]);
    await component.requestMove(task, TaskState.Done);

    await component.confirmMoveToDone();

    expect(taskService.transitionState).toHaveBeenCalledWith('task-1', TaskState.Done);
    expect(component.pendingDoneTask()).toBeNull();
  });

  it('cancelMoveToDone clears the prompt without moving', async () => {
    const { component } = createComponent();
    await component.requestMove(task, TaskState.Done);

    component.cancelMoveToDone();

    expect(component.pendingDoneTask()).toBeNull();
    expect(taskService.transitionState).not.toHaveBeenCalled();
  });

  it('archiveTask archives the task, reloads, and shows a success toast', async () => {
    const { component } = createComponent();
    taskService.archive.and.resolveTo(undefined);
    taskService.getBoardTasks.and.resolveTo([]);

    await component.archiveTask({ ...task, state: TaskState.Done });

    expect(taskService.archive).toHaveBeenCalledWith('task-1');
    expect(taskService.getBoardTasks).toHaveBeenCalled();
    expect(toast.success).toHaveBeenCalled();
  });

  it('toggleShowArchived flips the flag and refetches with includeArchived', async () => {
    const { component } = createComponent();
    taskService.getBoardTasks.and.resolveTo([]);

    await component.toggleShowArchived();

    expect(component.showArchived()).toBeTrue();
    expect(taskService.getBoardTasks).toHaveBeenCalledWith('board-1', true);
  });

  it('moveTask shows a success toast on success', async () => {
    const { component } = createComponent();
    taskService.transitionState.and.resolveTo(undefined);
    taskService.getBoardTasks.and.resolveTo([]);

    await component.moveTask(task, TaskState.InProgress);

    expect(toast.success).toHaveBeenCalled();
  });

  it('moveTask shows an error toast when the transition fails', async () => {
    const { component } = createComponent();
    taskService.transitionState.and.rejectWith(new Error('boom'));

    await component.moveTask(task, TaskState.InProgress);

    expect(toast.error).toHaveBeenCalledWith(jasmine.stringContaining('Could not move'));
  });

  it('assignToSelf assigns the task to the current user', async () => {
    const { component } = createComponent();
    taskService.assign.and.resolveTo(undefined);
    taskService.getBoardTasks.and.resolveTo([]);

    await component.assignToSelf(task);

    expect(taskService.assign).toHaveBeenCalledWith('task-1', 'user-1');
  });

  it('assignTo assigns the task to the chosen user and reloads the board', async () => {
    const { component } = createComponent();
    taskService.assign.and.resolveTo(undefined);
    taskService.getBoardTasks.and.resolveTo([]);

    await component.assignTo(task, 'user-2');

    expect(taskService.assign).toHaveBeenCalledWith('task-1', 'user-2');
    expect(taskService.getBoardTasks).toHaveBeenCalled();
  });

  it('assignTo does nothing when no user is chosen', async () => {
    const { component } = createComponent();

    await component.assignTo(task, '');

    expect(taskService.assign).not.toHaveBeenCalled();
  });

  it('assignTo shows an error toast when it fails', async () => {
    const { component } = createComponent();
    taskService.assign.and.rejectWith(new Error('boom'));

    await component.assignTo(task, 'user-2');

    expect(toast.error).toHaveBeenCalledWith(jasmine.stringContaining('Could not assign'));
  });

  it('assigneeName resolves the display name from the loaded members, or null when unassigned', async () => {
    const { component } = createComponent();
    taskService.getBoardTasks.and.resolveTo([]);
    boardService.getMembers.and.resolveTo([owner]);
    await component.ngOnInit();

    expect(component.assigneeName(task)).toBeNull();
    expect(component.assigneeName({ ...task, assigneeId: 'user-1' })).toBe('Ada');
    expect(component.assigneeName({ ...task, assigneeId: 'unknown' })).toBe('Unknown user');
  });

  it('assigneeColor resolves the member color, or null when unassigned/unknown', async () => {
    const { component } = createComponent();
    taskService.getBoardTasks.and.resolveTo([]);
    boardService.getMembers.and.resolveTo([owner]);
    await component.ngOnInit();

    expect(component.assigneeColor(task)).toBeNull();
    expect(component.assigneeColor({ ...task, assigneeId: 'user-1' })).toBe('#4fd1c5');
    expect(component.assigneeColor({ ...task, assigneeId: 'unknown' })).toBeNull();
  });

  it('saveTask (create mode) creates the task, resets the form and reloads the board', async () => {
    const { component } = createComponent();
    taskService.create.and.resolveTo('task-2');
    taskService.getBoardTasks.and.resolveTo([]);
    component.openNewTask();
    component.newTaskTitle = 'Write tests';
    component.newTaskDescription = 'For the board detail component';

    await component.saveTask();

    expect(taskService.create).toHaveBeenCalledWith('board-1', {
      title: 'Write tests',
      description: 'For the board detail component',
      priority: TaskPriority.Medium,
      dueAtUtc: null
    });
    expect(component.newTaskTitle).toBe('');
    expect(component.isTaskFormOpen()).toBeFalse();
  });

  it('openEditTask pre-fills the form and saveTask updates the task', async () => {
    const { component } = createComponent();
    taskService.update.and.resolveTo(undefined);
    taskService.getBoardTasks.and.resolveTo([]);

    component.openEditTask({ ...task, id: 'task-9', title: 'Old', priority: TaskPriority.Low });
    expect(component.editingTaskId()).toBe('task-9');
    expect(component.newTaskTitle).toBe('Old');
    expect(component.newTaskPriority).toBe(TaskPriority.Low);

    component.newTaskTitle = 'New title';
    await component.saveTask();

    expect(taskService.update).toHaveBeenCalledWith('task-9', jasmine.objectContaining({ title: 'New title' }));
    expect(taskService.create).not.toHaveBeenCalled();
    expect(component.editingTaskId()).toBeNull();
    expect(component.isTaskFormOpen()).toBeFalse();
  });

  it('saveTask does nothing when the title is blank', async () => {
    const { component } = createComponent();
    component.openNewTask();
    component.newTaskTitle = '   ';

    await component.saveTask();

    expect(taskService.create).not.toHaveBeenCalled();
  });

  it('saveTask rejects a past due date on create with a clear message, without calling the API', async () => {
    const { component } = createComponent();
    component.openNewTask();
    component.newTaskTitle = 'Ship it';
    component.newTaskDueDate = '2000-01-01'; // firmly in the past

    await component.saveTask();

    expect(taskService.create).not.toHaveBeenCalled();
    expect(component.errorMessage()).toContain('past');
  });

  it('saveTask surfaces the server validation message on a 400', async () => {
    const { component } = createComponent();
    taskService.create.and.rejectWith(
      new HttpErrorResponse({
        status: 400,
        error: { errors: { DueAtUtc: ['Due date cannot be in the past.'] } }
      })
    );
    component.openNewTask();
    component.newTaskTitle = 'Ship it';

    await component.saveTask();

    expect(component.errorMessage()).toBe('Due date cannot be in the past.');
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

  it('polls the member list every 20s so a newly-accepted teammate shows up without a reload', fakeAsync(() => {
    const { component } = createComponent();
    boardService.getMembers.and.resolveTo([owner]);
    taskService.getBoardTasks.and.resolveTo([]);

    component.ngOnInit();
    tick();
    expect(boardService.getMembers).toHaveBeenCalledTimes(1);

    tick(20_000);
    expect(boardService.getMembers).toHaveBeenCalledTimes(2);

    tick(20_000);
    expect(boardService.getMembers).toHaveBeenCalledTimes(3);

    component.ngOnDestroy();
    tick(20_000);
    expect(boardService.getMembers).toHaveBeenCalledTimes(3);
  }));

  it('isOwner is true when the current user is an Owner member of the board', async () => {
    const { component } = createComponent();
    boardService.getMembers.and.resolveTo([owner]);
    taskService.getBoardTasks.and.resolveTo([]);

    await component.ngOnInit();

    expect(component.isOwner()).toBeTrue();
  });

  it('isOwner is false when the current user is a Member (or not on the board)', async () => {
    const { component } = createComponent();
    boardService.getMembers.and.resolveTo([{ ...owner, role: BoardRole.Member }]);
    taskService.getBoardTasks.and.resolveTo([]);

    await component.ngOnInit();

    expect(component.isOwner()).toBeFalse();
  });

  it('inviteMember invites the entered email, resets the form and shows a confirmation', async () => {
    const { component } = createComponent();
    boardService.inviteMember.and.resolveTo(undefined);
    component.newMemberEmail = 'teammate@example.com';

    await component.inviteMember();

    expect(boardService.inviteMember).toHaveBeenCalledWith('board-1', { email: 'teammate@example.com' });
    expect(component.newMemberEmail).toBe('');
    expect(component.inviteSuccessMessage()).toBe('Invitation sent to teammate@example.com.');
  });

  it('inviteMember does nothing when the email is blank', async () => {
    const { component } = createComponent();

    await component.inviteMember();

    expect(boardService.inviteMember).not.toHaveBeenCalled();
  });

  it('inviteMember sets an error message when it fails', async () => {
    const { component } = createComponent();
    boardService.inviteMember.and.rejectWith(new Error('boom'));
    component.newMemberEmail = 'teammate@example.com';

    await component.inviteMember();

    expect(component.errorMessage()).toContain('Could not invite');
  });

  it('changeMemberRole updates the role and reloads the member list', async () => {
    const { component } = createComponent();
    boardService.updateMemberRole.and.resolveTo(undefined);
    boardService.getMembers.and.resolveTo([{ ...owner, role: BoardRole.Owner }]);

    await component.changeMemberRole('user-2', BoardRole.Owner);

    expect(boardService.updateMemberRole).toHaveBeenCalledWith('board-1', 'user-2', { role: BoardRole.Owner });
    expect(boardService.getMembers).toHaveBeenCalled();
  });

  it('changeMemberRole sets an error message when it fails', async () => {
    const { component } = createComponent();
    boardService.updateMemberRole.and.rejectWith(new Error('boom'));

    await component.changeMemberRole('user-2', BoardRole.Owner);

    expect(component.errorMessage()).toContain("Could not change this member's role");
  });

  it('removeMember removes the member and reloads the list', async () => {
    const { component } = createComponent();
    boardService.removeMember.and.resolveTo(undefined);
    boardService.getMembers.and.resolveTo([]);

    await component.removeMember('user-2');

    expect(boardService.removeMember).toHaveBeenCalledWith('board-1', 'user-2');
    expect(boardService.getMembers).toHaveBeenCalled();
  });

  it('removeMember sets an error message when it fails', async () => {
    const { component } = createComponent();
    boardService.removeMember.and.rejectWith(new Error('boom'));

    await component.removeMember('user-2');

    expect(component.errorMessage()).toContain('Could not remove this member');
  });
});
