import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { environment } from '../../../environments/environment';
import { CreateTaskRequest, TaskDto, TaskState } from '../models/task.model';

@Injectable({ providedIn: 'root' })
export class TaskService {
  private readonly baseUrl = environment.apiUrl;

  constructor(private readonly http: HttpClient) {}

  getBoardTasks(boardId: string): Promise<TaskDto[]> {
    return firstValueFrom(this.http.get<TaskDto[]>(`${this.baseUrl}/boards/${boardId}/tasks`));
  }

  create(boardId: string, request: CreateTaskRequest): Promise<string> {
    return firstValueFrom(this.http.post<string>(`${this.baseUrl}/boards/${boardId}/tasks`, request));
  }

  transitionState(taskId: string, newState: TaskState): Promise<void> {
    return firstValueFrom(
      this.http.patch<void>(`${this.baseUrl}/tasks/${taskId}/state`, { newState })
    );
  }

  assign(taskId: string, userId: string): Promise<void> {
    return firstValueFrom(
      this.http.patch<void>(`${this.baseUrl}/tasks/${taskId}/assignee`, { userId })
    );
  }
}
