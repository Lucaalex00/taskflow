import { test, expect } from '@playwright/test';
import { registerUser } from './helpers';

/**
 * Exercises the same end-to-end path the README's "Try the full workflow" section walks
 * through with curl, but through the real UI in two separate browser contexts (one per
 * identity — auth state lives in localStorage, so each needs its own context): invite by
 * email, accept from the notification bell, assign a task, and verify the role boundary
 * (a Member can't create or self-assign tasks) actually holds in the UI, not just the API.
 */
test('owner invites a teammate who accepts, gets assigned a task, and cannot create tasks themselves', async ({ browser }) => {
  const ownerContext = await browser.newContext();
  const ownerPage = await ownerContext.newPage();
  const owner = await registerUser(ownerPage, 'Owner Example');

  const memberContext = await browser.newContext();
  const memberPage = await memberContext.newPage();
  const member = await registerUser(memberPage, 'Teammate Example');

  const boardName = `Team Board ${Date.now()}`;
  await ownerPage.getByPlaceholder('New board name').fill(boardName);
  await ownerPage.getByRole('button', { name: '+ New board' }).click();
  await ownerPage.locator('.board-card', { hasText: boardName }).click();
  await expect(ownerPage).toHaveURL(/\/boards\/[0-9a-f-]+$/);

  // Owner invites the teammate by email — wait for the form to clear (it does so only after
  // the invite request succeeds) before moving on, so the invitation definitely exists before
  // the teammate looks for it.
  await ownerPage.getByRole('button', { name: /Members/ }).click();
  const inviteInput = ownerPage.getByPlaceholder('teammate@example.com');
  await inviteInput.fill(member.email);
  await ownerPage.getByRole('button', { name: 'Invite' }).click();
  await expect(inviteInput).toHaveValue('');

  // Teammate sees and accepts the invitation from the notification bell. The bell only
  // fetches on load and then polls every 20s (see notification-bell.component.ts), so reload
  // rather than wait out the poll interval.
  await memberPage.reload();
  await memberPage.locator('.notification-bell__toggle').click();
  await expect(memberPage.getByText(`invited to join the board "${boardName}"`)).toBeVisible();
  await memberPage.getByRole('button', { name: 'Accept', exact: true }).click();

  // Owner's member list was fetched before the teammate accepted, so it doesn't know about
  // them yet — reload to pick up the now-accepted membership before assigning.
  await ownerPage.reload();

  // Owner creates a task and assigns it to the teammate.
  await ownerPage.getByRole('button', { name: '+ New task' }).click();
  const taskTitle = `Draft the launch plan ${Date.now()}`;
  await ownerPage.getByLabel('Title').fill(taskTitle);
  await ownerPage.getByRole('button', { name: 'Create task' }).click();

  const taskCard = ownerPage.locator('.task-card', { hasText: taskTitle });
  await expect(taskCard).toBeVisible();
  await taskCard.locator('select').selectOption({ label: member.displayName });
  await expect(taskCard.getByText(`Assigned to ${member.displayName}`)).toBeVisible();

  // Teammate opens the newly-joined board (it shows up in their list without a page reload —
  // see docs/2026-07-23-owner-only-task-creation-and-board-ownership-display.md) and sees the
  // assigned task.
  await memberPage.locator('.notification-bell__toggle').click();
  await expect(memberPage.locator('.board-card', { hasText: boardName })).toBeVisible();
  await memberPage.locator('.board-card', { hasText: boardName }).click();
  const memberTaskCard = memberPage.locator('.task-card', { hasText: taskTitle });
  await expect(memberTaskCard).toBeVisible();
  await expect(memberTaskCard.getByText(`Assigned to ${member.displayName}`)).toBeVisible();

  // A Member never sees task-creation or self/other-assignment controls — those are Owner-only.
  await expect(memberPage.getByRole('button', { name: '+ New task' })).toHaveCount(0);
  await expect(memberTaskCard.locator('select')).toHaveCount(0);

  // But a Member CAN move their own assigned task through the state machine.
  await memberTaskCard.getByRole('button', { name: /In progress/ }).click();
  await expect(memberPage.locator('.column', { hasText: 'In progress' }).getByText(taskTitle)).toBeVisible();
});
