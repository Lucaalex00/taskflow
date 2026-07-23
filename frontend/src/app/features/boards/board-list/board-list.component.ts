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

  // Registration form state (shown once per browser, see CurrentUserService).
  registerEmail = '';
  registerDisplayName = '';
  readonly isRegistering = signal(false);

  // New board form state.
  newBoardName = '';
  readonly isCreatingBoard = signal(false);

  constructor(
    readonly currentUser: CurrentUserService,
    private readonly boardService: BoardService,
    private readonly router: Router
  ) {}

  async ngOnInit(): Promise<void> {
    if (this.currentUser.isRegistered()) {
      await this.loadBoards();
    } else {
      this.isLoading.set(false);
    }
  }

  async register(): Promise<void> {
    if (!this.registerEmail.trim() || !this.registerDisplayName.trim()) {
      return;
    }

    this.isRegistering.set(true);
    this.errorMessage.set(null);

    try {
      await this.currentUser.register({
        email: this.registerEmail.trim(),
        displayName: this.registerDisplayName.trim()
      });
      await this.loadBoards();
    } catch {
      this.errorMessage.set('Could not create your profile. Check the email format and try again.');
    } finally {
      this.isRegistering.set(false);
    }
  }

  async createBoard(): Promise<void> {
    const ownerId = this.currentUser.userId();
    if (!ownerId || !this.newBoardName.trim()) {
      return;
    }

    this.isCreatingBoard.set(true);
    this.errorMessage.set(null);

    try {
      await this.boardService.create({ name: this.newBoardName.trim(), ownerId });
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
