import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { BoardListComponent } from './board-list.component';
import { BoardService } from '../../../core/services/board.service';
import { CurrentUserService } from '../../../core/services/current-user.service';
import { BoardDto } from '../../../core/models/board.model';

describe('BoardListComponent', () => {
  let boardService: jasmine.SpyObj<BoardService>;
  let currentUser: jasmine.SpyObj<CurrentUserService>;
  let router: jasmine.SpyObj<Router>;

  const board: BoardDto = {
    id: 'board-1',
    name: 'Sprint 1',
    ownerId: 'user-1',
    taskCount: 2,
    createdAtUtc: '2026-01-01T00:00:00Z'
  };

  function createComponent(isRegistered: boolean) {
    boardService = jasmine.createSpyObj<BoardService>('BoardService', ['getAll', 'create']);
    currentUser = jasmine.createSpyObj<CurrentUserService>('CurrentUserService', [
      'register',
      'signOut',
      'isRegistered',
      'userId',
      'displayName'
    ]);
    currentUser.isRegistered.and.returnValue(isRegistered);
    currentUser.userId.and.returnValue(isRegistered ? 'user-1' : null);
    currentUser.displayName.and.returnValue(null);
    router = jasmine.createSpyObj<Router>('Router', ['navigate']);

    TestBed.configureTestingModule({
      imports: [BoardListComponent],
      providers: [
        { provide: BoardService, useValue: boardService },
        { provide: CurrentUserService, useValue: currentUser },
        { provide: Router, useValue: router }
      ]
    });

    const fixture = TestBed.createComponent(BoardListComponent);
    return { fixture, component: fixture.componentInstance };
  }

  it('loads boards on init when the user is already registered', async () => {
    const { component } = createComponent(true);
    boardService.getAll.and.resolveTo([board]);

    await component.ngOnInit();

    expect(boardService.getAll).toHaveBeenCalled();
    expect(component.boards()).toEqual([board]);
    expect(component.isLoading()).toBeFalse();
  });

  it('skips loading boards on init when the user is not registered', async () => {
    const { component } = createComponent(false);

    await component.ngOnInit();

    expect(boardService.getAll).not.toHaveBeenCalled();
    expect(component.isLoading()).toBeFalse();
  });

  it('register creates the profile and loads boards on success', async () => {
    const { component } = createComponent(false);
    currentUser.register.and.resolveTo('user-1');
    boardService.getAll.and.resolveTo([board]);
    component.registerEmail = 'ada@example.com';
    component.registerDisplayName = 'Ada';

    await component.register();

    expect(currentUser.register).toHaveBeenCalledWith({ email: 'ada@example.com', displayName: 'Ada' });
    expect(boardService.getAll).toHaveBeenCalled();
    expect(component.boards()).toEqual([board]);
    expect(component.errorMessage()).toBeNull();
  });

  it('register sets an error message when it fails', async () => {
    const { component } = createComponent(false);
    currentUser.register.and.rejectWith(new Error('boom'));
    component.registerEmail = 'ada@example.com';
    component.registerDisplayName = 'Ada';

    await component.register();

    expect(component.errorMessage()).toContain('Could not create your profile');
    expect(component.isRegistering()).toBeFalse();
  });

  it('register does nothing when the form is incomplete', async () => {
    const { component } = createComponent(false);
    component.registerEmail = '';
    component.registerDisplayName = 'Ada';

    await component.register();

    expect(currentUser.register).not.toHaveBeenCalled();
  });

  it('createBoard creates the board and reloads the list on success', async () => {
    const { component } = createComponent(true);
    boardService.create.and.resolveTo('board-2');
    boardService.getAll.and.resolveTo([board]);
    component.newBoardName = 'Sprint 2';

    await component.createBoard();

    expect(boardService.create).toHaveBeenCalledWith({ name: 'Sprint 2', ownerId: 'user-1' });
    expect(component.newBoardName).toBe('');
    expect(boardService.getAll).toHaveBeenCalled();
  });

  it('createBoard sets an error message when it fails', async () => {
    const { component } = createComponent(true);
    boardService.create.and.rejectWith(new Error('boom'));
    component.newBoardName = 'Sprint 2';

    await component.createBoard();

    expect(component.errorMessage()).toContain('Could not create the board');
    expect(component.isCreatingBoard()).toBeFalse();
  });

  it('openBoard navigates to the board detail route', () => {
    const { component } = createComponent(true);

    component.openBoard('board-1');

    expect(router.navigate).toHaveBeenCalledWith(['/boards', 'board-1']);
  });
});
