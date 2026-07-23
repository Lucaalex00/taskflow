import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { signal } from '@angular/core';
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
    ownerDisplayName: 'Ada',
    color: '#4fd1c5',
    taskCount: 2,
    createdAtUtc: '2026-01-01T00:00:00Z'
  };

  function createComponent() {
    boardService = jasmine.createSpyObj<BoardService>('BoardService', ['getAll', 'create', 'refresh']);
    // A real signal, like the service exposes — component reads it directly, so tests drive
    // it through refresh()'s fake implementation, the same way the service would.
    (boardService as unknown as { boards: ReturnType<typeof signal<BoardDto[]>> }).boards = signal<BoardDto[]>([]);
    boardService.refresh.and.resolveTo(undefined);
    currentUser = jasmine.createSpyObj<CurrentUserService>('CurrentUserService', [
      'signOut',
      'userId',
      'displayName'
    ]);
    currentUser.userId.and.returnValue('user-1');
    currentUser.displayName.and.returnValue('Ada');
    router = jasmine.createSpyObj<Router>('Router', ['navigate', 'navigateByUrl']);

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

  it('loads boards on init', async () => {
    const { component } = createComponent();
    boardService.refresh.and.callFake(async () => boardService.boards.set([board]));

    await component.ngOnInit();

    expect(boardService.refresh).toHaveBeenCalled();
    expect(component.boards()).toEqual([board]);
    expect(component.isLoading()).toBeFalse();
  });

  it('sets an error message when loading boards fails', async () => {
    const { component } = createComponent();
    boardService.refresh.and.rejectWith(new Error('boom'));

    await component.ngOnInit();

    expect(component.errorMessage()).toContain('Could not reach the API');
  });

  it('createBoard creates the board and reloads the list on success', async () => {
    const { component } = createComponent();
    boardService.create.and.resolveTo('board-2');
    component.newBoardName = 'Sprint 2';
    component.newBoardColor = '#63b3ed';

    await component.createBoard();

    expect(boardService.create).toHaveBeenCalledWith({ name: 'Sprint 2', color: '#63b3ed' });
    expect(component.newBoardName).toBe('');
    expect(boardService.refresh).toHaveBeenCalled();
  });

  it('createBoard sets an error message when it fails', async () => {
    const { component } = createComponent();
    boardService.create.and.rejectWith(new Error('boom'));
    component.newBoardName = 'Sprint 2';

    await component.createBoard();

    expect(component.errorMessage()).toContain('Could not create the board');
    expect(component.isCreatingBoard()).toBeFalse();
  });

  it('openBoard navigates to the board detail route', () => {
    const { component } = createComponent();

    component.openBoard('board-1');

    expect(router.navigate).toHaveBeenCalledWith(['/boards', 'board-1']);
  });

  it('isOwnBoard is true only for boards owned by the current user', () => {
    const { component } = createComponent();

    expect(component.isOwnBoard(board)).toBeTrue();
    expect(component.isOwnBoard({ ...board, ownerId: 'someone-else' })).toBeFalse();
  });

  it('signOut signs the user out and redirects to /login', () => {
    const { component } = createComponent();

    component.signOut();

    expect(currentUser.signOut).toHaveBeenCalled();
    expect(router.navigateByUrl).toHaveBeenCalledWith('/login');
  });
});
