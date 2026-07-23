import { Injectable, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { environment } from '../../../environments/environment';
import { NotificationDto } from '../models/notification.model';

@Injectable({ providedIn: 'root' })
export class NotificationService {
  private readonly baseUrl = `${environment.apiUrl}/notifications`;

  /** Kept here (rather than re-fetched by every consumer) so the bell badge and the dropdown
   * list always agree, and refresh() is the single place that updates both. */
  readonly notifications = signal<NotificationDto[]>([]);

  constructor(private readonly http: HttpClient) {}

  async refresh(): Promise<void> {
    this.notifications.set(await firstValueFrom(this.http.get<NotificationDto[]>(this.baseUrl)));
  }

  async markRead(notificationId: string): Promise<void> {
    await firstValueFrom(this.http.post<void>(`${this.baseUrl}/${notificationId}/read`, {}));
    this.notifications.update((current) =>
      current.map((n) => (n.id === notificationId ? { ...n, isRead: true } : n))
    );
  }

  async respondToInvitation(invitationId: string, accept: boolean): Promise<void> {
    await firstValueFrom(
      this.http.post<void>(`${environment.apiUrl}/invitations/${invitationId}/respond`, { accept })
    );
    await this.refresh();
  }
}
