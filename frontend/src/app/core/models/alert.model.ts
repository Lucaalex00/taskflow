export enum AlertSeverity {
  Info = 'Info',
  Warning = 'Warning',
  Critical = 'Critical'
}

export interface AlertDto {
  id: string;
  boardId: string;
  severity: AlertSeverity;
  message: string;
  relatedUserId: string | null;
  isRead: boolean;
  createdAtUtc: string;
}

/** Shape pushed over the "AlertRaised" SignalR event — mirrors SignalRAlertNotifier's payload. */
export interface RealtimeAlert {
  id: string;
  boardId: string;
  severity: AlertSeverity;
  message: string;
  relatedUserId: string | null;
  createdAtUtc: string;
}
