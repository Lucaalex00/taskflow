import { Component, computed } from '@angular/core';
import { ThemeService } from '../../core/services/theme.service';

/** Header control that flips between the dark "console" theme and the light one. */
@Component({
  selector: 'app-theme-toggle',
  standalone: true,
  templateUrl: './theme-toggle.component.html',
  styleUrl: './theme-toggle.component.scss'
})
export class ThemeToggleComponent {
  readonly isDark = computed(() => this.theme.theme() === 'dark');
  readonly label = computed(() => (this.isDark() ? 'Switch to light theme' : 'Switch to dark theme'));

  constructor(private readonly theme: ThemeService) {}

  toggle(): void {
    this.theme.toggle();
  }
}
