import { Page, expect } from '@playwright/test';

/** A per-test-run-unique email, so re-running the suite against a stack whose Postgres
 * volume wasn't wiped (`docker compose down` without `-v`) never collides with a prior run. */
export function uniqueEmail(label: string): string {
  return `${label}-${Date.now()}-${Math.floor(Math.random() * 1_000_000)}@example.com`;
}

export interface RegisteredUser {
  email: string;
  displayName: string;
  password: string;
}

/** Registers a brand-new user through the real UI and waits for the redirect to the board list. */
export async function registerUser(page: Page, displayName: string): Promise<RegisteredUser> {
  const user: RegisteredUser = {
    email: uniqueEmail(displayName.toLowerCase().replace(/\s+/g, '-')),
    displayName,
    password: 'Correct-horse-battery-staple9'
  };

  await page.goto('/login');
  await page.locator('.mode-switch').getByRole('button', { name: 'Register' }).click();
  await page.getByPlaceholder('you@example.com').fill(user.email);
  await page.getByPlaceholder('Ada Lovelace').fill(user.displayName);
  await page.locator('input[name="password"]').fill(user.password);
  await page.locator('form.onboard__form').getByRole('button').click();

  await expect(page.getByRole('heading', { name: 'Boards' })).toBeVisible();
  return user;
}

/** Signs out via the shell-header user menu (the avatar opens a menu holding "Sign out"). */
export async function signOut(page: Page): Promise<void> {
  await page.locator('.user-menu__trigger').click();
  await page.getByRole('button', { name: 'Sign out' }).click();
}

/** Signs in as an already-registered user through the real UI. Assumes the login form defaults
 * to sign-in mode (it does — "Register" must be actively selected to switch away from it). */
export async function loginAs(page: Page, user: RegisteredUser): Promise<void> {
  await page.goto('/login');
  await page.getByPlaceholder('you@example.com').fill(user.email);
  await page.locator('input[name="password"]').fill(user.password);
  await page.locator('form.onboard__form').getByRole('button').click();

  await expect(page.getByRole('heading', { name: 'Boards' })).toBeVisible();
}
