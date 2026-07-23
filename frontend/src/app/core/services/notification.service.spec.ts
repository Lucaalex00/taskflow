import { TestBed, fakeAsync, tick } from '@angular/core/testing';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { NotificationService } from './notification.service';
import { InvitationStatus, NotificationDto, NotificationType } from '../models/notification.model';

describe('NotificationService', () => {
  let service: NotificationService;
  let httpMock: HttpTestingController;

  const notification: NotificationDto = {
    id: 'notification-1',
    type: NotificationType.TaskAssigned,
    message: 'You were assigned a task.',
    boardId: 'board-1',
    taskId: 'task-1',
    invitationId: null,
    invitationStatus: null,
    isRead: false,
    createdAtUtc: '2026-01-01T00:00:00Z'
  };

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()]
    });
    service = TestBed.inject(NotificationService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('refresh fetches notifications and updates the signal', async () => {
    const promise = service.refresh();

    const req = httpMock.expectOne(`${environment.apiUrl}/notifications`);
    expect(req.request.method).toBe('GET');
    req.flush([notification]);

    await promise;
    expect(service.notifications()).toEqual([notification]);
  });

  it('markRead posts to the read endpoint and updates the local signal', async () => {
    service.notifications.set([notification]);

    const promise = service.markRead('notification-1');

    const req = httpMock.expectOne(`${environment.apiUrl}/notifications/notification-1/read`);
    expect(req.request.method).toBe('POST');
    req.flush(null);

    await promise;
    expect(service.notifications()[0].isRead).toBeTrue();
  });

  it('respondToInvitation posts the response and refreshes notifications', fakeAsync(() => {
    service.respondToInvitation('invitation-1', true);
    tick();

    const respondReq = httpMock.expectOne(`${environment.apiUrl}/invitations/invitation-1/respond`);
    expect(respondReq.request.method).toBe('POST');
    expect(respondReq.request.body).toEqual({ accept: true });
    respondReq.flush(null);
    tick();

    const refreshReq = httpMock.expectOne(`${environment.apiUrl}/notifications`);
    refreshReq.flush([{ ...notification, invitationStatus: InvitationStatus.Accepted }]);
    tick();

    expect(service.notifications()[0].invitationStatus).toBe(InvitationStatus.Accepted);
  }));
});
