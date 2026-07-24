import { Component, OnDestroy, OnInit, computed, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { NotificationService } from '../../../core/services/notification.service';
import { BoardService } from '../../../core/services/board.service';
import { NotificationType, InvitationStatus } from '../../../core/models/notification.model';

const POLL_INTERVAL_MS = 20_000;

/** Per-type presentation, so an invitation reads differently at a glance from a task update.
 * `tone` maps to a CSS modifier that colors the left accent bar and the type chip. */
const TYPE_META: Record<NotificationType, { label: string; icon: string; tone: string }> = {
  [NotificationType.BoardInvitation]: { label: 'Invitation', icon: '✉', tone: 'invite' },
  [NotificationType.TaskAssigned]: { label: 'Assigned', icon: '◆', tone: 'assigned' },
  [NotificationType.TaskStateChanged]: { label: 'Update', icon: '↻', tone: 'update' }
};

@Component({
  selector: 'app-notification-bell',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './notification-bell.component.html',
  styleUrl: './notification-bell.component.scss'
})
export class NotificationBellComponent implements OnInit, OnDestroy {
  readonly NotificationType = NotificationType;
  readonly InvitationStatus = InvitationStatus;

  readonly isOpen = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly unreadCount = computed(() => this.notificationService.notifications().filter((n) => !n.isRead).length);

  private pollHandle?: ReturnType<typeof setInterval>;

  constructor(
    readonly notificationService: NotificationService,
    private readonly boardService: BoardService
  ) {}

  ngOnInit(): void {
    void this.notificationService.refresh();
    this.pollHandle = setInterval(() => void this.notificationService.refresh(), POLL_INTERVAL_MS);
  }

  ngOnDestroy(): void {
    if (this.pollHandle) clearInterval(this.pollHandle);
  }

  openDrawer(): void {
    this.isOpen.set(true);
  }

  closeDrawer(): void {
    this.isOpen.set(false);
  }

  typeMeta(type: NotificationType): { label: string; icon: string; tone: string } {
    return TYPE_META[type];
  }

  async markRead(notificationId: string): Promise<void> {
    await this.notificationService.markRead(notificationId);
  }

  async markAllRead(): Promise<void> {
    this.errorMessage.set(null);
    try {
      await this.notificationService.markAllRead();
    } catch {
      this.errorMessage.set('Could not mark all as read.');
    }
  }

  async respond(invitationId: string, accept: boolean): Promise<void> {
    this.errorMessage.set(null);
    try {
      await this.notificationService.respondToInvitation(invitationId, accept);
      if (accept) {
        // The accepted board wasn't in the list before — refresh it wherever it's shown.
        await this.boardService.refresh();
      }
    } catch {
      this.errorMessage.set('Could not respond to this invitation.');
    }
  }
}
