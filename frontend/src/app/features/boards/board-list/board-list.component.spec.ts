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

  function createComponent() {
    boardService = jasmine.createSpyObj<BoardService>('BoardService', ['getAll', 'create']);
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
    boardService.getAll.and.resolveTo([board]);

    await component.ngOnInit();

    expect(boardService.getAll).toHaveBeenCalled();
    expect(component.boards()).toEqual([board]);
    expect(component.isLoading()).toBeFalse();
  });

  it('sets an error message when loading boards fails', async () => {
    const { component } = createComponent();
    boardService.getAll.and.rejectWith(new Error('boom'));

    await component.ngOnInit();

    expect(component.errorMessage()).toContain('Could not reach the API');
  });

  it('createBoard creates the board and reloads the list on success', async () => {
    const { component } = createComponent();
    boardService.create.and.resolveTo('board-2');
    boardService.getAll.and.resolveTo([board]);
    component.newBoardName = 'Sprint 2';

    await component.createBoard();

    expect(boardService.create).toHaveBeenCalledWith({ name: 'Sprint 2', ownerId: 'user-1' });
    expect(component.newBoardName).toBe('');
    expect(boardService.getAll).toHaveBeenCalled();
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

  it('signOut signs the user out and redirects to /login', () => {
    const { component } = createComponent();

    component.signOut();

    expect(currentUser.signOut).toHaveBeenCalled();
    expect(router.navigateByUrl).toHaveBeenCalledWith('/login');
  });
});
