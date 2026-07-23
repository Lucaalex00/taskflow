import { TestBed, fakeAsync, tick } from '@angular/core/testing';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import * as signalR from '@microsoft/signalr';
import { environment } from '../../../environments/environment';
import { AlertService } from './alert.service';
import { AlertSeverity, AlertDto, RealtimeAlert } from '../models/alert.model';

/** Minimal fake standing in for the real signalR.HubConnection in tests. */
class FakeHubConnection {
  private handlers = new Map<string, (payload: unknown) => void>();
  private reconnectedHandler: (() => void) | null = null;

  start = jasmine.createSpy('start').and.resolveTo(undefined);
  stop = jasmine.createSpy('stop').and.resolveTo(undefined);
  invoke = jasmine.createSpy('invoke').and.resolveTo(undefined);

  on(event: string, handler: (payload: unknown) => void): void {
    this.handlers.set(event, handler);
  }

  onreconnected(handler: () => void): void {
    this.reconnectedHandler = handler;
  }

  emit(event: string, payload: unknown): void {
    this.handlers.get(event)?.(payload);
  }

  reconnect(): void {
    this.reconnectedHandler?.();
  }
}

describe('AlertService', () => {
  let service: AlertService;
  let httpMock: HttpTestingController;
  let fakeConnection: FakeHubConnection;

  function expectBoardAlertsRequest(boardId: string) {
    return httpMock.expectOne((r) => r.url === `${environment.apiUrl}/boards/${boardId}/alerts`);
  }

  const alert: AlertDto = {
    id: 'alert-1',
    boardId: 'board-1',
    severity: AlertSeverity.Warning,
    message: 'Too many tasks in progress',
    relatedUserId: 'user-1',
    isRead: false,
    createdAtUtc: '2026-01-01T00:00:00Z'
  };

  beforeEach(() => {
    fakeConnection = new FakeHubConnection();
    spyOn(signalR.HubConnectionBuilder.prototype, 'withUrl').and.returnValue({
      withAutomaticReconnect: () => ({
        build: () => fakeConnection
      })
    } as unknown as signalR.HubConnectionBuilder);

    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()]
    });
    service = TestBed.inject(AlertService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('getBoardAlerts requests alerts for the given board', async () => {
    const promise = service.getBoardAlerts('board-1');

    const req = httpMock.expectOne(
      (r) => r.url === `${environment.apiUrl}/boards/board-1/alerts` && r.params.get('unreadOnly') === 'false'
    );
    expect(req.request.method).toBe('GET');
    req.flush([alert]);

    expect(await promise).toEqual([alert]);
  });

  it('markRead patches the alert as read', async () => {
    const promise = service.markRead('alert-1');

    const req = httpMock.expectOne(`${environment.apiUrl}/alerts/alert-1/read`);
    expect(req.request.method).toBe('PATCH');
    req.flush(null);

    await promise;
  });

  it('connectToBoard starts the hub connection, joins the board and loads its alerts', fakeAsync(() => {
    service.connectToBoard('board-1');
    tick();

    const req = httpMock.expectOne(
      (r) => r.url === `${environment.apiUrl}/boards/board-1/alerts`
    );
    req.flush([alert]);
    tick();

    expect(fakeConnection.start).toHaveBeenCalled();
    expect(fakeConnection.invoke).toHaveBeenCalledWith('JoinBoard', 'board-1');
    expect(service.connectionState()).toBe(signalR.HubConnectionState.Connected);
    expect(service.alerts()).toEqual([alert]);
  }));

  it('prepends incoming AlertRaised events to the alerts signal', fakeAsync(() => {
    service.connectToBoard('board-1');
    tick();
    expectBoardAlertsRequest('board-1').flush([alert]);
    tick();

    const pushed: RealtimeAlert = {
      id: 'alert-2',
      boardId: 'board-1',
      severity: AlertSeverity.Critical,
      message: 'Board load spiked',
      relatedUserId: null,
      createdAtUtc: '2026-01-02T00:00:00Z'
    };
    fakeConnection.emit('AlertRaised', pushed);

    expect(service.alerts()[0]).toEqual({ ...pushed, isRead: false });
    expect(service.alerts().length).toBe(2);
  }));

  it('switching boards leaves the previous board group before joining the new one', fakeAsync(() => {
    service.connectToBoard('board-1');
    tick();
    expectBoardAlertsRequest('board-1').flush([]);
    tick();

    service.connectToBoard('board-2');
    tick();
    expectBoardAlertsRequest('board-2').flush([]);
    tick();

    expect(fakeConnection.invoke).toHaveBeenCalledWith('LeaveBoard', 'board-1');
    expect(fakeConnection.invoke).toHaveBeenCalledWith('JoinBoard', 'board-2');
  }));

  it('disconnect stops the connection and resets state', fakeAsync(() => {
    service.connectToBoard('board-1');
    tick();
    expectBoardAlertsRequest('board-1').flush([alert]);
    tick();

    service.disconnect();
    tick();

    expect(fakeConnection.stop).toHaveBeenCalled();
    expect(service.connectionState()).toBe(signalR.HubConnectionState.Disconnected);
  }));
});
