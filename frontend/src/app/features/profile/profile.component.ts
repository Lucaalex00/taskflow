import { Component, computed, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { RouterLink } from '@angular/router';
import { CurrentUserService } from '../../core/services/current-user.service';
import { ThemeService, Theme } from '../../core/services/theme.service';
import { ToastService } from '../../core/services/toast.service';
import { AvatarComponent } from '../../shared/avatar/avatar.component';

/** Mirrors the backend's PasswordRules, exactly as the registration form does. */
const MIN_PASSWORD_LENGTH = 10;

interface PasswordRequirement {
  readonly label: string;
  readonly met: boolean;
}

/**
 * Everything about "you" in one place: identity (name, avatar colour) and credentials, plus
 * the appearance choice. Each section saves on its own — renaming yourself shouldn't require
 * retyping a password, and vice versa.
 */
@Component({
  selector: 'app-profile',
  standalone: true,
  imports: [FormsModule, RouterLink, AvatarComponent],
  templateUrl: './profile.component.html',
  styleUrl: './profile.component.scss'
})
export class ProfileComponent {
  displayName = '';
  currentPassword = '';

  readonly newPassword = signal('');
  readonly isSavingProfile = signal(false);
  readonly isSavingPassword = signal(false);
  readonly isSavingColor = signal(false);
  readonly profileError = signal<string | null>(null);
  readonly passwordError = signal<string | null>(null);

  readonly passwordRequirements = computed<PasswordRequirement[]>(() => {
    const value = this.newPassword();
    return [
      { label: `At least ${MIN_PASSWORD_LENGTH} characters`, met: value.length >= MIN_PASSWORD_LENGTH },
      { label: 'One uppercase letter', met: /[A-Z]/.test(value) },
      { label: 'One lowercase letter', met: /[a-z]/.test(value) },
      { label: 'One number', met: /[0-9]/.test(value) }
    ];
  });

  readonly isNewPasswordValid = computed(() => this.passwordRequirements().every((r) => r.met));
  readonly theme = computed(() => this.themeService.theme());

  constructor(
    readonly currentUser: CurrentUserService,
    private readonly themeService: ThemeService,
    private readonly toasts: ToastService
  ) {
    this.displayName = currentUser.displayName() ?? '';
  }

  async saveProfile(): Promise<void> {
    const name = this.displayName.trim();
    if (!name || this.isSavingProfile()) return;

    this.isSavingProfile.set(true);
    this.profileError.set(null);

    try {
      await this.currentUser.updateDisplayName(name);
      this.toasts.success('Profile updated.');
    } catch (error) {
      this.profileError.set(this.detailOf(error) ?? 'Could not save your profile.');
    } finally {
      this.isSavingProfile.set(false);
    }
  }

  async onColorInput(event: Event): Promise<void> {
    const color = (event.target as HTMLInputElement).value;
    this.isSavingColor.set(true);
    this.profileError.set(null);

    try {
      await this.currentUser.updateColor(color);
    } catch (error) {
      this.profileError.set(this.detailOf(error) ?? 'Could not save your color.');
    } finally {
      this.isSavingColor.set(false);
    }
  }

  async changePassword(): Promise<void> {
    if (!this.currentPassword || !this.isNewPasswordValid() || this.isSavingPassword()) return;

    this.isSavingPassword.set(true);
    this.passwordError.set(null);

    try {
      await this.currentUser.changePassword(this.currentPassword, this.newPassword());
      this.currentPassword = '';
      this.newPassword.set('');
      this.toasts.success('Password changed.');
    } catch (error) {
      this.passwordError.set(this.detailOf(error) ?? 'Could not change your password.');
    } finally {
      this.isSavingPassword.set(false);
    }
  }

  selectTheme(theme: Theme): void {
    this.themeService.set(theme);
  }

  /** Surfaces the server's own wording — "Current password is incorrect." beats a generic
   * failure. A 400 from the validation pipeline carries the useful text under `errors`
   * (ProblemDetails.detail is the generic "One or more validation failures occurred."), so
   * per-field messages come first and `detail` is the fallback for everything else. */
  private detailOf(error: unknown): string | null {
    if (!(error instanceof HttpErrorResponse)) return null;

    const errors = error.error?.errors as Record<string, string[]> | undefined;
    const firstFieldMessage = errors && Object.values(errors).flat().find((message) => !!message);

    return firstFieldMessage ?? (error.error?.detail as string | undefined) ?? null;
  }
}
