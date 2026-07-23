import { Injectable, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import * as signalR from '@microsoft/signalr';
import { environment } from '../../../environments/environment';
import { AlertDto, RealtimeAlert } from '../models/alert.model';

@Injectable({ providedIn: 'root' })
export class AlertService {
  private readonly baseUrl = environment.apiUrl;
  private hubConnection: signalR.HubConnection | null = null;
  private joinedBoardId: string | null = null;

  /** Live-updating list of alerts for whichever board is currently joined. */
  readonly alerts = signal<AlertDto[]>([]);
  readonly connectionState = signal<signalR.HubConnectionState>(signalR.HubConnectionState.Disconnected);

  constructor(private readonly http: HttpClient) {}

  async getBoardAlerts(boardId: string, unreadOnly = false): Promise<AlertDto[]> {
    return firstValueFrom(
      this.http.get<AlertDto[]>(`${this.baseUrl}/boards/${boardId}/alerts`, {
        params: { unreadOnly: String(unreadOnly) }
      })
    );
  }

  markRead(alertId: string): Promise<void> {
    return firstValueFrom(this.http.patch<void>(`${this.baseUrl}/alerts/${alertId}/read`, {}));
  }

  /**
   * Connects to the AlertsHub (once) and joins the given board's group, replacing
   * whichever board group was previously joined. Incoming "AlertRaised" events are
   * prepended to the `alerts` signal so components can bind to it directly.
   */
  async connectToBoard(boardId: string): Promise<void> {
    if (!this.hubConnection) {
      this.hubConnection = new signalR.HubConnectionBuilder()
        .withUrl(environment.hubUrl)
        .withAutomaticReconnect()
        .build();

      this.hubConnection.onreconnected(() => {
        this.connectionState.set(signalR.HubConnectionState.Connected);
        if (this.joinedBoardId) {
          this.hubConnection?.invoke('JoinBoard', this.joinedBoardId);
        }
      });

      this.hubConnection.on('AlertRaised', (payload: RealtimeAlert) => {
        this.alerts.update((current) => [
          { ...payload, isRead: false } as AlertDto,
          ...current
        ]);
      });

      await this.hubConnection.start();
      this.connectionState.set(signalR.HubConnectionState.Connected);
    }

    if (this.joinedBoardId && this.joinedBoardId !== boardId) {
      await this.hubConnection.invoke('LeaveBoard', this.joinedBoardId);
    }

    await this.hubConnection.invoke('JoinBoard', boardId);
    this.joinedBoardId = boardId;

    this.alerts.set(await this.getBoardAlerts(boardId));
  }

  async disconnect(): Promise<void> {
    if (this.hubConnection) {
      await this.hubConnection.stop();
      this.hubConnection = null;
      this.joinedBoardId = null;
      this.connectionState.set(signalR.HubConnectionState.Disconnected);
    }
  }
}
