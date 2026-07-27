import { test, expect, Page } from '@playwright/test';
import { registerUser } from './helpers';

/** Drives an Angular CDK drag from a task card into a target column. CDK starts a drag only
 * after the pointer moves a few pixels, so we nudge before moving to the destination. */
async function dragCardToColumn(page: Page, cardTitle: string, columnState: string): Promise<void> {
  const card = page.locator('.task-card', { hasText: cardTitle });
  const target = page.locator(`#column-${columnState}`);
  const cb = await card.boundingBox();
  const tb = await target.boundingBox();
  if (!cb || !tb) throw new Error('card or column not found');

  await page.mouse.move(cb.x + cb.width / 2, cb.y + 20);
  await page.mouse.down();
  await page.mouse.move(cb.x + cb.width / 2 + 10, cb.y + 30, { steps: 5 });
  await page.mouse.move(tb.x + tb.width / 2, tb.y + tb.height / 2, { steps: 10 });
  await page.mouse.move(tb.x + tb.width / 2, tb.y + tb.height / 2 + 5, { steps: 3 });
  await page.mouse.up();
}

async function openFreshBoard(page: Page, taskTitle: string): Promise<void> {
  await registerUser(page, 'DnD User');
  await page.getByPlaceholder('New board name').fill(`DnD ${Date.now()}`);
  await page.getByRole('button', { name: '+ New board' }).click();
  await page.locator('.board-card').first().click();
  await page.getByRole('button', { name: '+ New task' }).click();
  await page.getByLabel('Title').fill(taskTitle);
  await page.getByRole('button', { name: 'Create task' }).click();
  await expect(page.locator('.task-card', { hasText: taskTitle })).toBeVisible();
}

test.describe('Drag & drop', () => {
  test('dragging a card into a valid column moves the task there', async ({ page }) => {
    await openFreshBoard(page, 'Draggable task');

    await dragCardToColumn(page, 'Draggable task', 'InProgress');

    await expect(
      page.locator('.column[data-state="InProgress"] .task-card', { hasText: 'Draggable task' })
    ).toBeVisible();
    await expect(
      page.locator('.column[data-state="Todo"] .task-card', { hasText: 'Draggable task' })
    ).toHaveCount(0);
  });

  test('a card cannot be dropped into a column with no valid transition (Todo → Done)', async ({ page }) => {
    await openFreshBoard(page, 'Stuck task');

    // Todo → Done is not a valid transition, so that column isn't a connected drop target:
    // the card snaps back and stays in To do.
    await dragCardToColumn(page, 'Stuck task', 'Done');

    await expect(
      page.locator('.column[data-state="Todo"] .task-card', { hasText: 'Stuck task' })
    ).toBeVisible();
    await expect(
      page.locator('.column[data-state="Done"] .task-card', { hasText: 'Stuck task' })
    ).toHaveCount(0);
  });
});
