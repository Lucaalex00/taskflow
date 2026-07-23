import { TestBed, fakeAsync, tick } from '@angular/core/testing';
import { NotificationBellComponent } from './notification-bell.component';
import { NotificationService } from '../../../core/services/notification.service';
import { InvitationStatus, NotificationDto, NotificationType } from '../../../core/models/notification.model';
import { signal } from '@angular/core';

describe('NotificationBellComponent', () => {
  let notificationService: {
    notifications: ReturnType<typeof signal<NotificationDto[]>>;
    refresh: jasmine.Spy;
    markRead: jasmine.Spy;
    respondToInvitation: jasmine.Spy;
  };

  const pendingInvitation: NotificationDto = {
    id: 'notification-1',
    type: NotificationType.BoardInvitation,
    message: 'You were invited to Sprint 1.',
    boardId: 'board-1',
    taskId: null,
    invitationId: 'invitation-1',
    invitationStatus: InvitationStatus.Pending,
    isRead: false,
    createdAtUtc: '2026-01-01T00:00:00Z'
  };

  function createComponent() {
    notificationService = {
      notifications: signal<NotificationDto[]>([]),
      refresh: jasmine.createSpy('refresh').and.resolveTo(undefined),
      markRead: jasmine.createSpy('markRead').and.resolveTo(undefined),
      respondToInvitation: jasmine.createSpy('respondToInvitation').and.resolveTo(undefined)
    };

    TestBed.configureTestingModule({
      imports: [NotificationBellComponent],
      providers: [{ provide: NotificationService, useValue: notificationService }]
    });

    const fixture = TestBed.createComponent(NotificationBellComponent);
    return { fixture, component: fixture.componentInstance };
  }

  it('toggle flips isOpen', () => {
    const { component } = createComponent();

    component.toggle();
    expect(component.isOpen()).toBeTrue();

    component.toggle();
    expect(component.isOpen()).toBeFalse();
  });

  it('unreadCount counts only unread notifications', () => {
    const { component } = createComponent();
    notificationService.notifications.set([
      { ...pendingInvitation, isRead: false },
      { ...pendingInvitation, id: 'notification-2', isRead: true }
    ]);

    expect(component.unreadCount()).toBe(1);
  });

  it('ngOnInit refreshes notifications and polls periodically', fakeAsync(() => {
    const { component } = createComponent();

    component.ngOnInit();
    expect(notificationService.refresh).toHaveBeenCalledTimes(1);

    tick(20_000);
    expect(notificationService.refresh).toHaveBeenCalledTimes(2);

    component.ngOnDestroy();
    tick(20_000);
    expect(notificationService.refresh).toHaveBeenCalledTimes(2);
  }));

  it('open marks the notification as read', async () => {
    const { component } = createComponent();

    await component.open('notification-1');

    expect(notificationService.markRead).toHaveBeenCalledWith('notification-1');
  });

  it('respond accepts or declines an invitation', async () => {
    const { component } = createComponent();

    await component.respond('invitation-1', true);

    expect(notificationService.respondToInvitation).toHaveBeenCalledWith('invitation-1', true);
  });

  it('respond sets an error message when it fails', async () => {
    const { component } = createComponent();
    notificationService.respondToInvitation.and.rejectWith(new Error('boom'));

    await component.respond('invitation-1', true);

    expect(component.errorMessage()).toContain('Could not respond');
  });
});
