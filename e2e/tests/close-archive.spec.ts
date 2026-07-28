import { test, expect, Page } from '@playwright/test';
import { registerUser } from './helpers';

async function createTask(page: Page, title: string): Promise<void> {
  await page.getByRole('button', { name: '+ New task' }).click();
  await page.getByLabel('Title').fill(title);
  await page.getByRole('button', { name: 'Create task' }).click();
  await expect(page.locator('.task-card', { hasText: title })).toBeVisible();
}

test.describe('Close & archive', () => {
  test('moving to Done asks for confirmation, then the card locks with an archive X', async ({ page }) => {
    await registerUser(page, 'Closer');
    await page.getByPlaceholder('New board name').fill(`Close ${Date.now()}`);
    await page.getByRole('button', { name: '+ New board' }).click();
    await page.locator('.board-card').first().click();
    await createTask(page, 'Finish the report');

    const card = page.locator('.task-card', { hasText: 'Finish the report' });
    await card.getByRole('button', { name: /In progress/ }).click();

    // Requesting Done opens a confirmation dialog rather than moving immediately.
    await page.locator('.task-card', { hasText: 'Finish the report' })
      .getByRole('button', { name: /Done/ }).click();
    await expect(page.getByRole('dialog', { name: 'Confirm close task' })).toBeVisible();
    await page.getByRole('button', { name: 'Yes, close it' }).click();

    // Now in Done, locked (not draggable → no cdkDrag handle behavior) and with an archive X.
    const doneCard = page.locator('.column[data-state="Done"] .task-card', { hasText: 'Finish the report' });
    await expect(doneCard).toBeVisible();
    await expect(doneCard.locator('.task-card__archive')).toBeVisible();

    // Archiving hides it from the board...
    await doneCard.locator('.task-card__archive').click();
    await expect(page.locator('.task-card', { hasText: 'Finish the report' })).toHaveCount(0);

    // ...until "show completed" is switched on in the filter panel.
    await page.locator('.board-filters__toggle').click();
    await page.getByText('Show completed (archived) tasks').click();
    const archivedCard = page.locator('.task-card--archived', { hasText: 'Finish the report' });
    await expect(archivedCard).toBeVisible();
    await expect(archivedCard.getByText('archived')).toBeVisible();
  });
});
