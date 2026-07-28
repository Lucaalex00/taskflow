import { Component, OnDestroy, OnInit, computed, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { CdkDragDrop, DragDropModule } from '@angular/cdk/drag-drop';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { TaskService } from '../../../core/services/task.service';
import { AlertService } from '../../../core/services/alert.service';
import { CurrentUserService } from '../../../core/services/current-user.service';
import { BoardService } from '../../../core/services/board.service';
import { ToastService } from '../../../core/services/toast.service';
import { TaskDto, TaskState, TaskPriority } from '../../../core/models/task.model';
import { AlertSeverity } from '../../../core/models/alert.model';
import { BoardMemberDto, BoardRole } from '../../../core/models/board.model';
import { AvatarComponent } from '../../../shared/avatar/avatar.component';

/** Mirrors TaskItem's state machine in the Domain layer (see TaskItem.IsValidTransition). */
const ALLOWED_TRANSITIONS: Record<TaskState, TaskState[]> = {
  [TaskState.Todo]: [TaskState.InProgress, TaskState.Cancelled],
  [TaskState.InProgress]: [TaskState.Blocked, TaskState.Done, TaskState.Todo, TaskState.Cancelled],
  [TaskState.Blocked]: [TaskState.InProgress, TaskState.Cancelled],
  [TaskState.Done]: [],
  [TaskState.Cancelled]: []
};

const BOARD_COLUMNS = [TaskState.Todo, TaskState.InProgress, TaskState.Blocked, TaskState.Done] as const;

// The member list has no push channel (unlike alerts/notifications), so a newly-accepted
// teammate wouldn't show up in the assignee dropdown until a manual reload. Poll instead, at
// the same cadence NotificationBellComponent already uses for the same reason.
const MEMBERS_POLL_INTERVAL_MS = 20_000;

const COLUMN_LABELS: Record<TaskState, string> = {
  [TaskState.Todo]: 'To do',
  [TaskState.InProgress]: 'In progress',
  [TaskState.Blocked]: 'Blocked',
  [TaskState.Done]: 'Done',
  [TaskState.Cancelled]: 'Cancelled'
};

/** Pulls the first human-readable validation message out of an API error's RFC 7807
 * ProblemDetails body (the `errors` map the ValidationBehavior fills, falling back to
 * `detail`), so the UI can show exactly what the server rejected. */
function validationMessageFrom(error: unknown): string | null {
  if (!(error instanceof HttpErrorResponse) || error.status !== 400) return null;
  const body = error.error as { errors?: Record<string, string[]>; detail?: string } | null;
  const firstError = body?.errors ? Object.values(body.errors)[0]?.[0] : undefined;
  return firstError ?? body?.detail ?? null;
}

@Component({
  selector: 'app-board-detail',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink, AvatarComponent, DragDropModule],
  templateUrl: './board-detail.component.html',
  styleUrl: './board-detail.component.scss'
})
export class BoardDetailComponent implements OnInit, OnDestroy {
  readonly boardId: string;
  readonly columns: TaskState[] = [...BOARD_COLUMNS];
  readonly columnLabels = COLUMN_LABELS;
  readonly TaskPriority = TaskPriority;
  readonly AlertSeverity = AlertSeverity;
  readonly BoardRole = BoardRole;

  readonly tasks = signal<TaskDto[]>([]);
  readonly members = signal<BoardMemberDto[]>([]);
  readonly isLoading = signal(true);
  readonly showCancelled = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly isTaskFormOpen = signal(false);
  readonly isMembersOpen = signal(false);

  // Board filters. searchText matches title/description; assigneeFilter is a userId,
  // 'unassigned', or '' (all); priorityFilter is a TaskPriority or 'all'.
  readonly searchText = signal('');
  readonly assigneeFilter = signal<string>('');
  readonly priorityFilter = signal<TaskPriority | 'all'>('all');

  readonly hasActiveFilters = computed(
    () => this.searchText().trim() !== '' || this.assigneeFilter() !== '' || this.priorityFilter() !== 'all'
  );

  private readonly filteredTasks = computed(() => {
    const query = this.searchText().trim().toLowerCase();
    const assignee = this.assigneeFilter();
    const priority = this.priorityFilter();

    return this.tasks().filter((t) => {
      if (query && !`${t.title} ${t.description ?? ''}`.toLowerCase().includes(query)) return false;
      if (assignee === 'unassigned' && t.assigneeId) return false;
      if (assignee !== '' && assignee !== 'unassigned' && t.assigneeId !== assignee) return false;
      if (priority !== 'all' && t.priority !== priority) return false;
      return true;
    });
  });

  readonly tasksByColumn = computed(() => {
    const grouped = new Map<TaskState, TaskDto[]>();
    for (const column of BOARD_COLUMNS) {
      grouped.set(column, this.filteredTasks().filter((t) => t.state === column));
    }
    return grouped;
  });

  readonly visibleTaskCount = computed(
    () => this.filteredTasks().filter((t) => t.state !== TaskState.Cancelled).length
  );

  readonly cancelledCount = computed(
    () => this.tasks().filter((t) => t.state === TaskState.Cancelled).length
  );

  readonly isOwner = computed(
    () => this.members().find((m) => m.userId === this.currentUser.userId())?.role === BoardRole.Owner
  );

  // New task form state.
  newTaskTitle = '';
  newTaskDescription = '';
  newTaskPriority: TaskPriority = TaskPriority.Medium;
  newTaskDueDate = '';
  readonly isCreatingTask = signal(false);

  // Invite member form state.
  newMemberEmail = '';
  readonly isInvitingMember = signal(false);
  readonly inviteSuccessMessage = signal<string | null>(null);

  private membersPollHandle?: ReturnType<typeof setInterval>;

  constructor(
    private readonly route: ActivatedRoute,
    private readonly taskService: TaskService,
    private readonly boardService: BoardService,
    private readonly toast: ToastService,
    readonly alertService: AlertService,
    readonly currentUser: CurrentUserService
  ) {
    this.boardId = this.route.snapshot.paramMap.get('id')!;
  }

  async ngOnInit(): Promise<void> {
    await Promise.all([
      this.loadTasks(),
      this.loadMembers(),
      this.alertService.connectToBoard(this.boardId)
    ]);

    this.membersPollHandle = setInterval(() => void this.loadMembers(), MEMBERS_POLL_INTERVAL_MS);
  }

  async ngOnDestroy(): Promise<void> {
    if (this.membersPollHandle) clearInterval(this.membersPollHandle);
    await this.alertService.disconnect();
  }

  clearFilters(): void {
    this.searchText.set('');
    this.assigneeFilter.set('');
    this.priorityFilter.set('all');
  }

  transitionsFor(task: TaskDto): TaskState[] {
    return ALLOWED_TRANSITIONS[task.state];
  }

  /** The other columns this column's cards may be dragged into (a valid transition exists from
   * that column's state). Used to wire up cdkDropListConnectedTo so CDK only allows real moves. */
  connectedDropListsFor(column: TaskState): string[] {
    return BOARD_COLUMNS.filter((c) => c !== column && ALLOWED_TRANSITIONS[column].includes(c)).map(
      (c) => `column-${c}`
    );
  }

  async onTaskDropped(event: CdkDragDrop<TaskState>): Promise<void> {
    const target = event.container.data;
    const source = event.previousContainer.data;
    const task = event.item.data as TaskDto;

    // Same column, or a transition the domain wouldn't allow → snap back, do nothing.
    if (target === source || !ALLOWED_TRANSITIONS[source]?.includes(target)) {
      return;
    }

    await this.moveTask(task, target);
  }

  async moveTask(task: TaskDto, newState: TaskState): Promise<void> {
    try {
      await this.taskService.transitionState(task.id, newState);
      await this.loadTasks();
      this.toast.success(`Moved "${task.title}" to ${COLUMN_LABELS[newState]}.`);
    } catch {
      this.toast.error(`Could not move "${task.title}" to ${COLUMN_LABELS[newState]}.`);
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
      const assignee = this.members().find((m) => m.userId === userId)?.displayName;
      this.toast.success(assignee ? `Assigned "${task.title}" to ${assignee}.` : `Assigned "${task.title}".`);
    } catch {
      this.toast.error(`Could not assign "${task.title}".`);
    }
  }

  assigneeName(task: TaskDto): string | null {
    if (!task.assigneeId) return null;
    return this.members().find((m) => m.userId === task.assigneeId)?.displayName ?? 'Unknown user';
  }

  assigneeColor(task: TaskDto): string | null {
    if (!task.assigneeId) return null;
    return this.members().find((m) => m.userId === task.assigneeId)?.color ?? null;
  }

  /** Today's date as yyyy-MM-dd, for the due-date input's `min` (blocks past dates in the picker). */
  get minDueDate(): string {
    return new Date().toISOString().split('T')[0];
  }

  async createTask(): Promise<void> {
    if (!this.newTaskTitle.trim()) return;

    // Catch a past due date before hitting the server, with a message pointing at the field.
    if (this.newTaskDueDate && this.newTaskDueDate < this.minDueDate) {
      this.errorMessage.set('Due date cannot be in the past — pick today or a later date.');
      return;
    }

    this.isCreatingTask.set(true);
    this.errorMessage.set(null);

    try {
      await this.taskService.create(this.boardId, {
        title: this.newTaskTitle.trim(),
        description: this.newTaskDescription.trim() || null,
        priority: this.newTaskPriority,
        dueAtUtc: this.newTaskDueDate ? new Date(this.newTaskDueDate).toISOString() : null
      });

      const createdTitle = this.newTaskTitle.trim();
      this.newTaskTitle = '';
      this.newTaskDescription = '';
      this.newTaskPriority = TaskPriority.Medium;
      this.newTaskDueDate = '';
      this.isTaskFormOpen.set(false);

      await this.loadTasks();
      this.toast.success(`Task "${createdTitle}" created.`);
    } catch (error) {
      // Prefer the server's specific validation message (e.g. "Due date cannot be in the
      // past.") over a generic one, so the user knows exactly what to fix.
      this.errorMessage.set(
        validationMessageFrom(error) ?? 'Could not create the task. Check the title and due date.'
      );
    } finally {
      this.isCreatingTask.set(false);
    }
  }

  async inviteMember(): Promise<void> {
    if (!this.newMemberEmail.trim()) return;

    const email = this.newMemberEmail.trim();
    this.isInvitingMember.set(true);
    this.errorMessage.set(null);
    this.inviteSuccessMessage.set(null);

    try {
      await this.boardService.inviteMember(this.boardId, { email });
      this.newMemberEmail = '';
      this.inviteSuccessMessage.set(`Invitation sent to ${email}.`);
    } catch {
      this.errorMessage.set('Could not invite this email (they may already be invited or a member).');
    } finally {
      this.isInvitingMember.set(false);
    }
  }

  async changeMemberRole(userId: string, role: BoardRole): Promise<void> {
    try {
      await this.boardService.updateMemberRole(this.boardId, userId, { role });
      await this.loadMembers();
    } catch {
      this.errorMessage.set('Could not change this member\'s role (a board must keep at least one owner).');
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
}
