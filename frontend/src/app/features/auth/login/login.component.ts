import { Component, OnInit, computed, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { Router } from '@angular/router';
import { CurrentUserService } from '../../../core/services/current-user.service';
import { AppConfigService, PublicConfig } from '../../../core/services/app-config.service';

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
export class LoginComponent implements OnInit {
  readonly mode = signal<Mode>('login');
  /** Non-null only on an instance that seeded one (see AppConfigService) — a deployment with
   * SEED_DEMO=false simply never renders the button. */
  readonly demoAccount = signal<PublicConfig | null>(null);
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
    private readonly appConfig: AppConfigService,
    private readonly router: Router
  ) {}

  async ngOnInit(): Promise<void> {
    const config = await this.appConfig.load();
    if (config.demoAccountAvailable) this.demoAccount.set(config);
  }

  /** One click into a populated workspace. Goes through the ordinary login endpoint with the
   * seeded account's real credentials — there is no separate "demo" authentication path. */
  async signInAsDemo(): Promise<void> {
    const demo = this.demoAccount();
    if (!demo?.demoEmail || !demo.demoPassword) return;

    this.isSubmitting.set(true);
    this.errorMessage.set(null);

    try {
      await this.currentUser.login({ email: demo.demoEmail, password: demo.demoPassword });
      await this.router.navigateByUrl('/');
    } catch {
      this.errorMessage.set('The demo account is unavailable on this instance.');
    } finally {
      this.isSubmitting.set(false);
    }
  }

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
    } catch (error) {
      this.errorMessage.set(this.messageFor(error));
    } finally {
      this.isSubmitting.set(false);
    }
  }

  /** The account-lockout case (too many failed logins) returns a specific server message worth
   * surfacing verbatim, so the user understands it's a temporary lock rather than wrong
   * credentials. Everything else stays intentionally vague. */
  private messageFor(error: unknown): string {
    if (this.mode() === 'login' && error instanceof HttpErrorResponse) {
      const detail = error.error?.detail as string | undefined;
      if (error.status === 401 && detail?.toLowerCase().includes('locked')) {
        return detail;
      }
      return 'Invalid email or password.';
    }

    return this.mode() === 'login'
      ? 'Invalid email or password.'
      : 'Could not create your account. Check the email format and password requirements.';
  }
}
