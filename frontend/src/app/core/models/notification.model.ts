export enum NotificationType {
  BoardInvitation = 'BoardInvitation',
  TaskAssigned = 'TaskAssigned',
  TaskStateChanged = 'TaskStateChanged'
}

export enum InvitationStatus {
  Pending = 'Pending',
  Accepted = 'Accepted',
  Declined = 'Declined'
}

export interface NotificationDto {
  id: string;
  type: NotificationType;
  message: string;
  boardId: string | null;
  taskId: string | null;
  invitationId: string | null;
  invitationStatus: InvitationStatus | null;
  isRead: boolean;
  createdAtUtc: string;
}
