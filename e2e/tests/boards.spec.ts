import { test, expect } from '@playwright/test';
import { registerUser } from './helpers';

test.describe('Boards', () => {
  test('creating a board shows it in the grid, owned by the creator', async ({ page }) => {
    await registerUser(page, 'Owner');

    const boardName = `Launch Plan ${Date.now()}`;
    await page.getByPlaceholder('New board name').fill(boardName);
    await page.getByRole('button', { name: '+ New board' }).click();

    const card = page.locator('.board-card', { hasText: boardName });
    await expect(card).toBeVisible();
    await expect(card.getByText('created by you')).toBeVisible();
  });

  test('opening a board shows its (empty) Kanban columns', async ({ page }) => {
    await registerUser(page, 'Owner');

    const boardName = `Empty Board ${Date.now()}`;
    await page.getByPlaceholder('New board name').fill(boardName);
    await page.getByRole('button', { name: '+ New board' }).click();
    await page.locator('.board-card', { hasText: boardName }).click();

    await expect(page).toHaveURL(/\/boards\/[0-9a-f-]+$/);
    await expect(page.getByText('To do')).toBeVisible();
    await expect(page.getByText('In progress')).toBeVisible();
    await expect(page.getByText('Blocked')).toBeVisible();
    await expect(page.getByText('Done')).toBeVisible();
  });
});
