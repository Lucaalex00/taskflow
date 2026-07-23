import { Component, OnDestroy, OnInit, computed, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { TaskService } from '../../../core/services/task.service';
import { AlertService } from '../../../core/services/alert.service';
import { CurrentUserService } from '../../../core/services/current-user.service';
import { UserService } from '../../../core/services/user.service';
import { BoardService } from '../../../core/services/board.service';
import { TaskDto, TaskState, TaskPriority } from '../../../core/models/task.model';
import { AlertSeverity } from '../../../core/models/alert.model';
import { UserDto } from '../../../core/models/user.model';
import { BoardMemberDto, BoardRole } from '../../../core/models/board.model';

/** Mirrors TaskItem's state machine in the Domain layer (see TaskItem.IsValidTransition). */
const ALLOWED_TRANSITIONS: Record<TaskState, TaskState[]> = {
  [TaskState.Todo]: [TaskState.InProgress, TaskState.Cancelled],
  [TaskState.InProgress]: [TaskState.Blocked, TaskState.Done, TaskState.Todo, TaskState.Cancelled],
  [TaskState.Blocked]: [TaskState.InProgress, TaskState.Cancelled],
  [TaskState.Done]: [],
  [TaskState.Cancelled]: []
};

const BOARD_COLUMNS = [TaskState.Todo, TaskState.InProgress, TaskState.Blocked, TaskState.Done] as const;

const COLUMN_LABELS: Record<TaskState, string> = {
  [TaskState.Todo]: 'To do',
  [TaskState.InProgress]: 'In progress',
  [TaskState.Blocked]: 'Blocked',
  [TaskState.Done]: 'Done',
  [TaskState.Cancelled]: 'Cancelled'
};

@Component({
  selector: 'app-board-detail',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './board-detail.component.html',
  styleUrl: './board-detail.component.scss'
})
export class BoardDetailComponent implements OnInit, OnDestroy {
  readonly boardId: string;
  readonly columns = BOARD_COLUMNS;
  readonly columnLabels = COLUMN_LABELS;
  readonly TaskPriority = TaskPriority;
  readonly AlertSeverity = AlertSeverity;
  readonly BoardRole = BoardRole;

  readonly tasks = signal<TaskDto[]>([]);
  readonly members = signal<BoardMemberDto[]>([]);
  readonly allUsers = signal<UserDto[]>([]);
  readonly isLoading = signal(true);
  readonly showCancelled = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly isTaskFormOpen = signal(false);
  readonly isMembersOpen = signal(false);

  readonly tasksByColumn = computed(() => {
    const grouped = new Map<TaskState, TaskDto[]>();
    for (const column of BOARD_COLUMNS) {
      grouped.set(column, this.tasks().filter((t) => t.state === column));
    }
    return grouped;
  });

  readonly cancelledCount = computed(
    () => this.tasks().filter((t) => t.state === TaskState.Cancelled).length
  );

  readonly isOwner = computed(
    () => this.members().find((m) => m.userId === this.currentUser.userId())?.role === BoardRole.Owner
  );

  /** Registered users not yet on this board — candidates for the "add member" picker. */
  readonly addableUsers = computed(() => {
    const memberIds = new Set(this.members().map((m) => m.userId));
    return this.allUsers().filter((u) => !memberIds.has(u.id));
  });

  // New task form state.
  newTaskTitle = '';
  newTaskDescription = '';
  newTaskPriority: TaskPriority = TaskPriority.Medium;
  newTaskDueDate = '';
  readonly isCreatingTask = signal(false);

  // Add member form state.
  newMemberUserId = '';
  newMemberRole: BoardRole = BoardRole.Member;
  readonly isAddingMember = signal(false);

  constructor(
    private readonly route: ActivatedRoute,
    private readonly taskService: TaskService,
    private readonly userService: UserService,
    private readonly boardService: BoardService,
    readonly alertService: AlertService,
    readonly currentUser: CurrentUserService
  ) {
    this.boardId = this.route.snapshot.paramMap.get('id')!;
  }

  async ngOnInit(): Promise<void> {
    await Promise.all([
      this.loadTasks(),
      this.loadMembers(),
      this.loadAllUsers(),
      this.alertService.connectToBoard(this.boardId)
    ]);
  }

  async ngOnDestroy(): Promise<void> {
    await this.alertService.disconnect();
  }

  transitionsFor(task: TaskDto): TaskState[] {
    return ALLOWED_TRANSITIONS[task.state];
  }

  async moveTask(task: TaskDto, newState: TaskState): Promise<void> {
    try {
      await this.taskService.transitionState(task.id, newState);
      await this.loadTasks();
    } catch {
      this.errorMessage.set(`Could not move "${task.title}" to ${COLUMN_LABELS[newState]}.`);
    }
  }

  async assignToSelf(task: TaskDto): Promise<void> {
    const userId = this.currentUser.userId();
    if (!userId) return;

    await this.assignTo(task, userId);
  }

  async assignTo(task: TaskDto, userId: string): Promise<void> {
    if (!userId) return;

    try {
      await this.taskService.assign(task.id, userId);
      await this.loadTasks();
    } catch {
      this.errorMessage.set(`Could not assign "${task.title}".`);
    }
  }

  assigneeName(task: TaskDto): string | null {
    if (!task.assigneeId) return null;
    return this.members().find((m) => m.userId === task.assigneeId)?.displayName ?? 'Unknown user';
  }

  async createTask(): Promise<void> {
    if (!this.newTaskTitle.trim()) return;

    this.isCreatingTask.set(true);
    this.errorMessage.set(null);

    try {
      await this.taskService.create(this.boardId, {
        title: this.newTaskTitle.trim(),
        description: this.newTaskDescription.trim() || null,
        priority: this.newTaskPriority,
        dueAtUtc: this.newTaskDueDate ? new Date(this.newTaskDueDate).toISOString() : null
      });

      this.newTaskTitle = '';
      this.newTaskDescription = '';
      this.newTaskPriority = TaskPriority.Medium;
      this.newTaskDueDate = '';
      this.isTaskFormOpen.set(false);

      await this.loadTasks();
    } catch {
      this.errorMessage.set('Could not create the task. Check the title and due date.');
    } finally {
      this.isCreatingTask.set(false);
    }
  }

  async addMember(): Promise<void> {
    if (!this.newMemberUserId) return;

    this.isAddingMember.set(true);
    this.errorMessage.set(null);

    try {
      await this.boardService.addMember(this.boardId, {
        userId: this.newMemberUserId,
        role: this.newMemberRole
      });
      this.newMemberUserId = '';
      this.newMemberRole = BoardRole.Member;
      await this.loadMembers();
    } catch {
      this.errorMessage.set('Could not add this member to the board.');
    } finally {
      this.isAddingMember.set(false);
    }
  }

  async removeMember(userId: string): Promise<void> {
    try {
      await this.boardService.removeMember(this.boardId, userId);
      await this.loadMembers();
    } catch {
      this.errorMessage.set('Could not remove this member (a board must keep at least one owner).');
    }
  }

  async markAlertRead(alertId: string): Promise<void> {
    await this.alertService.markRead(alertId);
    this.alertService.alerts.update((current) =>
      current.map((a) => (a.id === alertId ? { ...a, isRead: true } : a))
    );
  }

  private async loadTasks(): Promise<void> {
    this.isLoading.set(true);
    try {
      this.tasks.set(await this.taskService.getBoardTasks(this.boardId));
    } catch {
      this.errorMessage.set('Could not load tasks for this board.');
    } finally {
      this.isLoading.set(false);
    }
  }

  private async loadMembers(): Promise<void> {
    try {
      this.members.set(await this.boardService.getMembers(this.boardId));
    } catch {
      // Non-critical: the assignee dropdown and member list just stay empty.
    }
  }

  private async loadAllUsers(): Promise<void> {
    try {
      this.allUsers.set(await this.userService.getAll());
    } catch {
      // Non-critical: only affects the "add member" picker.
    }
  }
}
