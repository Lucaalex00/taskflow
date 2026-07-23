import { Component, OnDestroy, OnInit, computed, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { NotificationService } from '../../../core/services/notification.service';
import { NotificationType, InvitationStatus } from '../../../core/models/notification.model';

const POLL_INTERVAL_MS = 20_000;

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

  constructor(readonly notificationService: NotificationService) {}

  ngOnInit(): void {
    void this.notificationService.refresh();
    this.pollHandle = setInterval(() => void this.notificationService.refresh(), POLL_INTERVAL_MS);
  }

  ngOnDestroy(): void {
    if (this.pollHandle) clearInterval(this.pollHandle);
  }

  toggle(): void {
    this.isOpen.set(!this.isOpen());
  }

  async open(notificationId: string): Promise<void> {
    await this.notificationService.markRead(notificationId);
  }

  async respond(invitationId: string, accept: boolean): Promise<void> {
    this.errorMessage.set(null);
    try {
      await this.notificationService.respondToInvitation(invitationId, accept);
    } catch {
      this.errorMessage.set('Could not respond to this invitation.');
    }
  }
}
