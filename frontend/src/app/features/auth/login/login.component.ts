import { Component, computed, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { CurrentUserService } from '../../../core/services/current-user.service';

type Mode = 'login' | 'register';

interface PasswordRequirement {
  readonly label: string;
  readonly met: boolean;
}

/** Mirrors the backend's PasswordRules (Application/Common/Validation) so the UI shows the
 * same policy live, rather than only surfacing it as a 400 after submitting. */
const MIN_PASSWORD_LENGTH = 10;

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './login.component.html',
  styleUrl: './login.component.scss'
})
export class LoginComponent {
  readonly mode = signal<Mode>('login');
  readonly isSubmitting = signal(false);
  readonly errorMessage = signal<string | null>(null);

  readonly password = signal('');

  readonly passwordRequirements = computed<PasswordRequirement[]>(() => {
    const value = this.password();
    return [
      { label: `At least ${MIN_PASSWORD_LENGTH} characters`, met: value.length >= MIN_PASSWORD_LENGTH },
      { label: 'One uppercase letter', met: /[A-Z]/.test(value) },
      { label: 'One lowercase letter', met: /[a-z]/.test(value) },
      { label: 'One number', met: /[0-9]/.test(value) }
    ];
  });

  readonly isPasswordValid = computed(() => this.passwordRequirements().every((r) => r.met));

  email = '';
  displayName = '';

  constructor(
    private readonly currentUser: CurrentUserService,
    private readonly router: Router
  ) {}

  switchMode(mode: Mode): void {
    this.mode.set(mode);
    this.errorMessage.set(null);
  }

  async submit(): Promise<void> {
    if (!this.email.trim() || !this.password().trim()) return;
    if (this.mode() === 'register' && (!this.displayName.trim() || !this.isPasswordValid())) return;

    this.isSubmitting.set(true);
    this.errorMessage.set(null);

    try {
      if (this.mode() === 'login') {
        await this.currentUser.login({ email: this.email.trim(), password: this.password() });
      } else {
        await this.currentUser.register({
          email: this.email.trim(),
          displayName: this.displayName.trim(),
          password: this.password()
        });
      }

      await this.router.navigateByUrl('/');
    } catch {
      this.errorMessage.set(
        this.mode() === 'login'
          ? 'Invalid email or password.'
          : 'Could not create your account. Check the email format and password requirements.'
      );
    } finally {
      this.isSubmitting.set(false);
    }
  }
}
