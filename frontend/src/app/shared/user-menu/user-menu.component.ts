import { Component, signal } from '@angular/core';
import { Router } from '@angular/router';
import { CurrentUserService } from '../../core/services/current-user.service';
import { AvatarComponent } from '../avatar/avatar.component';

/** Shell-header control: the signed-in user's avatar opens a small menu to recolor the avatar
 * (any time) and sign out. */
@Component({
  selector: 'app-user-menu',
  standalone: true,
  imports: [AvatarComponent],
  templateUrl: './user-menu.component.html',
  styleUrl: './user-menu.component.scss'
})
export class UserMenuComponent {
  readonly isOpen = signal(false);
  readonly isSavingColor = signal(false);
  readonly errorMessage = signal<string | null>(null);

  constructor(
    readonly currentUser: CurrentUserService,
    private readonly router: Router
  ) {}

  toggle(): void {
    this.isOpen.update((open) => !open);
  }

  close(): void {
    this.isOpen.set(false);
  }

  async onColorInput(event: Event): Promise<void> {
    const color = (event.target as HTMLInputElement).value;
    this.isSavingColor.set(true);
    this.errorMessage.set(null);
    try {
      await this.currentUser.updateColor(color);
    } catch {
      this.errorMessage.set('Could not save your color.');
    } finally {
      this.isSavingColor.set(false);
    }
  }

  signOut(): void {
    this.close();
    this.currentUser.signOut();
    this.router.navigateByUrl('/login');
  }
}
