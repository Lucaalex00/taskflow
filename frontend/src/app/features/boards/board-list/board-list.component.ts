import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { BoardService } from '../../../core/services/board.service';
import { CurrentUserService } from '../../../core/services/current-user.service';
import { BoardDto } from '../../../core/models/board.model';

@Component({
  selector: 'app-board-list',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './board-list.component.html',
  styleUrl: './board-list.component.scss'
})
export class BoardListComponent implements OnInit {
  readonly boards = signal<BoardDto[]>([]);
  readonly isLoading = signal(true);
  readonly errorMessage = signal<string | null>(null);

  // New board form state.
  newBoardName = '';
  readonly isCreatingBoard = signal(false);

  constructor(
    readonly currentUser: CurrentUserService,
    private readonly boardService: BoardService,
    private readonly router: Router
  ) {}

  async ngOnInit(): Promise<void> {
    await this.loadBoards();
  }

  async createBoard(): Promise<void> {
    if (!this.newBoardName.trim()) {
      return;
    }

    this.isCreatingBoard.set(true);
    this.errorMessage.set(null);

    try {
      await this.boardService.create({ name: this.newBoardName.trim() });
      this.newBoardName = '';
      await this.loadBoards();
    } catch {
      this.errorMessage.set('Could not create the board. Try a different name.');
    } finally {
      this.isCreatingBoard.set(false);
    }
  }

  openBoard(boardId: string): void {
    this.router.navigate(['/boards', boardId]);
  }

  signOut(): void {
    this.currentUser.signOut();
    this.router.navigateByUrl('/login');
  }

  private async loadBoards(): Promise<void> {
    this.isLoading.set(true);
    try {
      this.boards.set(await this.boardService.getAll());
    } catch {
      this.errorMessage.set('Could not reach the API. Is the backend running?');
    } finally {
      this.isLoading.set(false);
    }
  }
}
