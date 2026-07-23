import { Injectable, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  BoardDto,
  BoardMemberDto,
  CreateBoardRequest,
  InviteBoardMemberRequest,
  UpdateBoardMemberRoleRequest
} from '../models/board.model';

@Injectable({ providedIn: 'root' })
export class BoardService {
  private readonly baseUrl = `${environment.apiUrl}/boards`;

  /** Shared across every consumer (board list, notification bell after accepting an
   * invitation, ...) so accepting an invite elsewhere is reflected without a page reload. */
  readonly boards = signal<BoardDto[]>([]);

  constructor(private readonly http: HttpClient) {}

  getAll(): Promise<BoardDto[]> {
    return firstValueFrom(this.http.get<BoardDto[]>(this.baseUrl));
  }

  async refresh(): Promise<void> {
    this.boards.set(await this.getAll());
  }

  create(request: CreateBoardRequest): Promise<string> {
    return firstValueFrom(this.http.post<string>(this.baseUrl, request));
  }

  getMembers(boardId: string): Promise<BoardMemberDto[]> {
    return firstValueFrom(this.http.get<BoardMemberDto[]>(`${this.baseUrl}/${boardId}/members`));
  }

  inviteMember(boardId: string, request: InviteBoardMemberRequest): Promise<void> {
    return firstValueFrom(this.http.post<void>(`${this.baseUrl}/${boardId}/invitations`, request));
  }

  updateMemberRole(boardId: string, userId: string, request: UpdateBoardMemberRoleRequest): Promise<void> {
    return firstValueFrom(this.http.patch<void>(`${this.baseUrl}/${boardId}/members/${userId}/role`, request));
  }

  removeMember(boardId: string, userId: string): Promise<void> {
    return firstValueFrom(this.http.delete<void>(`${this.baseUrl}/${boardId}/members/${userId}`));
  }
}
