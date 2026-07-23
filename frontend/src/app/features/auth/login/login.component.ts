import { Component, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { CurrentUserService } from '../../../core/services/current-user.service';

type Mode = 'login' | 'register';

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

  email = '';
  password = '';
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
    if (!this.email.trim() || !this.password.trim()) return;
    if (this.mode() === 'register' && !this.displayName.trim()) return;

    this.isSubmitting.set(true);
    this.errorMessage.set(null);

    try {
      if (this.mode() === 'login') {
        await this.currentUser.login({ email: this.email.trim(), password: this.password });
      } else {
        await this.currentUser.register({
          email: this.email.trim(),
          displayName: this.displayName.trim(),
          password: this.password
        });
      }

      await this.router.navigateByUrl('/');
    } catch {
      this.errorMessage.set(
        this.mode() === 'login'
          ? 'Invalid email or password.'
          : 'Could not create your account. Check the email format and try a longer password.'
      );
    } finally {
      this.isSubmitting.set(false);
    }
  }
}
