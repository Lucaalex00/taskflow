import { Injectable, computed, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AuthResult, CreateUserRequest, LoginRequest } from '../models/user.model';

const STORAGE_KEY_TOKEN = 'taskflow.authToken';
const STORAGE_KEY_USER_ID = 'taskflow.currentUserId';
const STORAGE_KEY_NAME = 'taskflow.currentUserName';

/**
 * Holds the signed-in user's identity and JWT. The token is attached to outgoing requests
 * by authInterceptor (see core/interceptors/auth.interceptor.ts) and read back by authGuard
 * to gate routes.
 */
@Injectable({ providedIn: 'root' })
export class CurrentUserService {
  private readonly tokenSignal = signal<string | null>(localStorage.getItem(STORAGE_KEY_TOKEN));
  private readonly userIdSignal = signal<string | null>(localStorage.getItem(STORAGE_KEY_USER_ID));
  private readonly userNameSignal = signal<string | null>(localStorage.getItem(STORAGE_KEY_NAME));

  readonly token = computed(() => this.tokenSignal());
  readonly userId = computed(() => this.userIdSignal());
  readonly displayName = computed(() => this.userNameSignal());
  readonly isAuthenticated = computed(() => this.tokenSignal() !== null);

  constructor(private readonly http: HttpClient) {}

  async register(request: CreateUserRequest): Promise<void> {
    const result = await firstValueFrom(
      this.http.post<AuthResult>(`${environment.apiUrl}/users`, request)
    );

    this.applyAuthResult(result);
  }

  async login(request: LoginRequest): Promise<void> {
    const result = await firstValueFrom(
      this.http.post<AuthResult>(`${environment.apiUrl}/auth/login`, request)
    );

    this.applyAuthResult(result);
  }

  signOut(): void {
    localStorage.removeItem(STORAGE_KEY_TOKEN);
    localStorage.removeItem(STORAGE_KEY_USER_ID);
    localStorage.removeItem(STORAGE_KEY_NAME);
    this.tokenSignal.set(null);
    this.userIdSignal.set(null);
    this.userNameSignal.set(null);
  }

  private applyAuthResult(result: AuthResult): void {
    localStorage.setItem(STORAGE_KEY_TOKEN, result.token);
    localStorage.setItem(STORAGE_KEY_USER_ID, result.userId);
    localStorage.setItem(STORAGE_KEY_NAME, result.displayName);
    this.tokenSignal.set(result.token);
    this.userIdSignal.set(result.userId);
    this.userNameSignal.set(result.displayName);
  }
}
