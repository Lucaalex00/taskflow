import { Injectable, computed, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AuthResult, CreateUserRequest, LoginRequest, UserDto } from '../models/user.model';

const STORAGE_KEY_TOKEN = 'taskflow.authToken';
const STORAGE_KEY_USER_ID = 'taskflow.currentUserId';
const STORAGE_KEY_NAME = 'taskflow.currentUserName';
const STORAGE_KEY_COLOR = 'taskflow.currentUserColor';

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
  private readonly colorSignal = signal<string | null>(localStorage.getItem(STORAGE_KEY_COLOR));

  readonly token = computed(() => this.tokenSignal());
  readonly userId = computed(() => this.userIdSignal());
  readonly displayName = computed(() => this.userNameSignal());
  readonly color = computed(() => this.colorSignal());
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

  /** Changes the signed-in user's avatar color and persists the new value locally so every
   * avatar (header, member lists, task cards) reflects it immediately. */
  async updateColor(color: string): Promise<void> {
    const result = await firstValueFrom(
      this.http.patch<UserDto>(`${environment.apiUrl}/users/me/color`, { color })
    );

    localStorage.setItem(STORAGE_KEY_COLOR, result.color);
    this.colorSignal.set(result.color);
  }

  /** Renames the signed-in user and keeps the locally cached name in step, so the header
   * avatar and every "created by you" label update without a reload. */
  async updateDisplayName(displayName: string): Promise<void> {
    const result = await firstValueFrom(
      this.http.patch<UserDto>(`${environment.apiUrl}/users/me`, { displayName })
    );

    localStorage.setItem(STORAGE_KEY_NAME, result.displayName);
    this.userNameSignal.set(result.displayName);
  }

  /** Changes the password. Deliberately does not touch the stored token: the existing JWT stays
   * valid until it expires, so changing your password doesn't sign you out mid-session. */
  async changePassword(currentPassword: string, newPassword: string): Promise<void> {
    await firstValueFrom(
      this.http.post<void>(`${environment.apiUrl}/users/me/password`, { currentPassword, newPassword })
    );
  }

  signOut(): void {
    localStorage.removeItem(STORAGE_KEY_TOKEN);
    localStorage.removeItem(STORAGE_KEY_USER_ID);
    localStorage.removeItem(STORAGE_KEY_NAME);
    localStorage.removeItem(STORAGE_KEY_COLOR);
    this.tokenSignal.set(null);
    this.userIdSignal.set(null);
    this.userNameSignal.set(null);
    this.colorSignal.set(null);
  }

  private applyAuthResult(result: AuthResult): void {
    localStorage.setItem(STORAGE_KEY_TOKEN, result.token);
    localStorage.setItem(STORAGE_KEY_USER_ID, result.userId);
    localStorage.setItem(STORAGE_KEY_NAME, result.displayName);
    localStorage.setItem(STORAGE_KEY_COLOR, result.color);
    this.tokenSignal.set(result.token);
    this.userIdSignal.set(result.userId);
    this.userNameSignal.set(result.displayName);
    this.colorSignal.set(result.color);
  }
}
