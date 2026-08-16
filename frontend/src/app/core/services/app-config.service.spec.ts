import { TestBed } from '@angular/core/testing';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { AppConfigService } from './app-config.service';

describe('AppConfigService', () => {
  let httpMock: HttpTestingController;
  let service: AppConfigService;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()]
    });
    httpMock = TestBed.inject(HttpTestingController);
    service = TestBed.inject(AppConfigService);
  });

  afterEach(() => httpMock.verify());

  it('returns the instance configuration from GET /config', async () => {
    const pending = service.load();

    const request = httpMock.expectOne(`${environment.apiUrl}/config`);
    expect(request.request.method).toBe('GET');
    request.flush({
      demoAccountAvailable: true,
      demoEmail: 'demo@taskflow.dev',
      demoPassword: 'Demo-password-2026'
    });

    await expectAsync(pending).toBeResolvedTo({
      demoAccountAvailable: true,
      demoEmail: 'demo@taskflow.dev',
      demoPassword: 'Demo-password-2026'
    });
  });

  it('falls back to "no demo account" when the endpoint fails, so login still works', async () => {
    const pending = service.load();

    httpMock.expectOne(`${environment.apiUrl}/config`).flush('nope', { status: 500, statusText: 'Server Error' });

    await expectAsync(pending).toBeResolvedTo({
      demoAccountAvailable: false,
      demoEmail: null,
      demoPassword: null
    });
  });
});
