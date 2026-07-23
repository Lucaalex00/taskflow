import { Injectable, computed, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { environment } from '../../../environments/environment';
import { CreateUserRequest } from '../models/user.model';

const STORAGE_KEY = 'taskflow.currentUserId';
const STORAGE_KEY_NAME = 'taskflow.currentUserName';

/**
 * Demo-scope identity: no real authentication is in the project brief, so this
 * simply persists a generated user id in memory + localStorage so the same
 * "person" is reused across page reloads during a demo session.
 */
@Injectable({ providedIn: 'root' })
export class CurrentUserService {
  private readonly userIdSignal = signal<string | null>(localStorage.getItem(STORAGE_KEY));
  private readonly userNameSignal = signal<string | null>(localStorage.getItem(STORAGE_KEY_NAME));

  readonly userId = computed(() => this.userIdSignal());
  readonly displayName = computed(() => this.userNameSignal());
  readonly isRegistered = computed(() => this.userIdSignal() !== null);

  constructor(private readonly http: HttpClient) {}

  async register(request: CreateUserRequest): Promise<string> {
    const userId = await firstValueFrom(
      this.http.post<string>(`${environment.apiUrl}/users`, request)
    );

    localStorage.setItem(STORAGE_KEY, userId);
    localStorage.setItem(STORAGE_KEY_NAME, request.displayName);
    this.userIdSignal.set(userId);
    this.userNameSignal.set(request.displayName);

    return userId;
  }

  signOut(): void {
    localStorage.removeItem(STORAGE_KEY);
    localStorage.removeItem(STORAGE_KEY_NAME);
    this.userIdSignal.set(null);
    this.userNameSignal.set(null);
  }
}
