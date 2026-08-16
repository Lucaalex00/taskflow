import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { environment } from '../../../environments/environment';

/** Mirrors PublicConfigDto (Application/Configuration/Queries/GetPublicConfig). */
export interface PublicConfig {
  readonly demoAccountAvailable: boolean;
  readonly demoEmail: string | null;
  readonly demoPassword: string | null;
}

const NO_DEMO: PublicConfig = { demoAccountAvailable: false, demoEmail: null, demoPassword: null };

/** Reads the instance's anonymous configuration — today only "is there a demo account to
 * sign in with", which the login screen uses to decide whether to offer one-click access. */
@Injectable({ providedIn: 'root' })
export class AppConfigService {
  constructor(private readonly http: HttpClient) {}

  /** Never rejects: the demo button is a convenience, and a failed config call should leave
   * the ordinary login form perfectly usable rather than breaking the page. */
  async load(): Promise<PublicConfig> {
    try {
      return await firstValueFrom(this.http.get<PublicConfig>(`${environment.apiUrl}/config`));
    } catch {
      return NO_DEMO;
    }
  }
}
