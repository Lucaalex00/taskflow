import { test, expect } from '@playwright/test';
import { registerUser, loginAs } from './helpers';

test.describe('Authentication', () => {
  test('an unauthenticated visitor is redirected to /login', async ({ page }) => {
    await page.goto('/');
    await expect(page).toHaveURL(/\/login$/);
  });

  test('register, sign out, and sign back in with the same credentials', async ({ page }) => {
    const user = await registerUser(page, 'Ada Lovelace');

    await page.getByRole('button', { name: 'Sign out' }).click();
    await expect(page).toHaveURL(/\/login$/);

    await loginAs(page, user);
    await expect(page.getByText(`signed in as ${user.displayName}`)).toBeVisible();
  });

  test('signing in with the wrong password shows an error and does not navigate away', async ({ page }) => {
    const user = await registerUser(page, 'Grace Hopper');
    await page.getByRole('button', { name: 'Sign out' }).click();

    await page.goto('/login');
    await page.getByPlaceholder('you@example.com').fill(user.email);
    await page.locator('input[name="password"]').fill('definitely-the-wrong-password');
    await page.locator('form.onboard__form').getByRole('button').click();

    await expect(page.locator('.form-error')).toBeVisible();
    await expect(page).toHaveURL(/\/login$/);
  });
});
