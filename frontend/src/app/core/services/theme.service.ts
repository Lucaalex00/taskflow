import { Injectable, signal } from '@angular/core';

export type Theme = 'dark' | 'light';

const STORAGE_KEY = 'taskflow.theme';

/**
 * Owns the one piece of UI state that isn't per-account: which theme this browser renders.
 * Applied by setting data-theme on <html> — every colour in the app comes from the tokens in
 * styles.scss, so flipping that attribute re-themes the whole UI with no per-component work.
 *
 * Stored locally rather than on the user, on purpose: it's a property of the device you're
 * looking at (a laptop in a dark room, a projector in a bright one), not of who you are.
 */
@Injectable({ providedIn: 'root' })
export class ThemeService {
  private readonly themeSignal = signal<Theme>(this.readInitialTheme());

  readonly theme = this.themeSignal.asReadonly();

  constructor() {
    this.apply(this.themeSignal());
  }

  toggle(): void {
    this.set(this.themeSignal() === 'dark' ? 'light' : 'dark');
  }

  set(theme: Theme): void {
    this.themeSignal.set(theme);
    localStorage.setItem(STORAGE_KEY, theme);
    this.apply(theme);
  }

  /** An explicit past choice wins; otherwise follow the OS preference, defaulting to the
   * dark "console" look the design was built around. */
  private readInitialTheme(): Theme {
    const stored = localStorage.getItem(STORAGE_KEY);
    if (stored === 'dark' || stored === 'light') return stored;

    return window.matchMedia?.('(prefers-color-scheme: light)').matches ? 'light' : 'dark';
  }

  private apply(theme: Theme): void {
    document.documentElement.setAttribute('data-theme', theme);
  }
}
