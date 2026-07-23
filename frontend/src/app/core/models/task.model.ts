export enum TaskState {
  Todo = 'Todo',
  InProgress = 'InProgress',
  Blocked = 'Blocked',
  Done = 'Done',
  Cancelled = 'Cancelled'
}

export enum TaskPriority {
  Low = 'Low',
  Medium = 'Medium',
  High = 'High',
  Critical = 'Critical'
}

export interface TaskDto {
  id: string;
  boardId: string;
  title: string;
  description: string | null;
  state: TaskState;
  priority: TaskPriority;
  assigneeId: string | null;
  dueAtUtc: string | null;
  isOverdue: boolean;
  createdAtUtc: string;
  updatedAtUtc: string;
}

export interface CreateTaskRequest {
  title: string;
  description: string | null;
  priority: TaskPriority;
  dueAtUtc: string | null;
}
