import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap } from '@angular/router';
import { signal } from '@angular/core';
import { BoardDetailComponent } from './board-detail.component';
import { TaskService } from '../../../core/services/task.service';
import { AlertService } from '../../../core/services/alert.service';
import { CurrentUserService } from '../../../core/services/current-user.service';
import { BoardService } from '../../../core/services/board.service';
import { TaskDto, TaskPriority, TaskState } from '../../../core/models/task.model';
import { AlertDto, AlertSeverity } from '../../../core/models/alert.model';
import { BoardMemberDto, BoardRole } from '../../../core/models/board.model';

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
      'transitionState',
      'assign'
    ]);
    boardService = jasmine.createSpyObj<BoardService>('BoardService', [
      'getAll',
      'create',
      'getMembers',
      'inviteMember',
      'updateMemberRole',
      'removeMember'
    ]);
    boardService.getMembers.and.resolveTo([]);
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
        { provide: BoardService, useValue: boardService },
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

  it('loads tasks, members and connects to the alerts hub on init', async () => {
    const { component } = createComponent();
    taskService.getBoardTasks.and.resolveTo([task]);
    boardService.getMembers.and.resolveTo([owner]);

    await component.ngOnInit();

    expect(taskService.getBoardTasks).toHaveBeenCalledWith('board-1');
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

  it('assignTo sets an error message when it fails', async () => {
    const { component } = createComponent();
    taskService.assign.and.rejectWith(new Error('boom'));

    await component.assignTo(task, 'user-2');

    expect(component.errorMessage()).toContain('Could not assign');
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

  it('inviteMember invites the entered email and resets the form', async () => {
    const { component } = createComponent();
    boardService.inviteMember.and.resolveTo(undefined);
    component.newMemberEmail = 'teammate@example.com';

    await component.inviteMember();

    expect(boardService.inviteMember).toHaveBeenCalledWith('board-1', { email: 'teammate@example.com' });
    expect(component.newMemberEmail).toBe('');
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
