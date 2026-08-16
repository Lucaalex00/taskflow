/**
 * Regenerates the screenshots embedded in README.md, driving the real app the same way the
 * E2E suite does — so the images can never drift into showing a UI that no longer exists.
 *
 * Requires the stack to be running with the demo workspace seeded (the default):
 *   docker compose up --build -d
 *   cd e2e && node capture-screenshots.mjs
 */
import { chromium } from '@playwright/test';
import { mkdir } from 'node:fs/promises';
import { fileURLToPath } from 'node:url';
import { join } from 'node:path';

const BASE_URL = process.env.E2E_BASE_URL ?? 'http://localhost:4200';
const OUT_DIR = fileURLToPath(new URL('../docs/screenshots/', import.meta.url));

const shots = [];

async function shot(page, name) {
  await page.waitForTimeout(400); // let transitions settle so the image isn't caught mid-fade
  await page.screenshot({ path: join(OUT_DIR, `${name}.png`) });
  shots.push(name);
}

const browser = await chromium.launch();
// Pin the colour scheme: the app follows the OS preference on a first visit, and a headless
// browser defaults to light — which would silently capture the "light theme" screenshots twice.
const page = await browser.newPage({ viewport: { width: 1440, height: 900 }, colorScheme: 'dark' });

await mkdir(OUT_DIR, { recursive: true });

await page.goto(BASE_URL);
await page.getByRole('button', { name: 'Explore the demo workspace' }).waitFor();
await shot(page, 'login');

await page.getByRole('button', { name: 'Explore the demo workspace' }).click();
await page.getByText('Product launch').waitFor();
await shot(page, 'board-list');

await page.getByText('Product launch').click();
await page.getByText('In progress').first().waitFor();
await shot(page, 'board-detail');

// Same board, light theme — the toggle lives in the shell header.
await page.getByRole('button', { name: /Switch to light theme/i }).click();
await shot(page, 'board-detail-light');
await page.getByRole('button', { name: /Switch to dark theme/i }).click();

await page.goto(`${BASE_URL}/profile`);
await page.getByText('Profile & settings').waitFor();
await shot(page, 'profile');

await browser.close();

console.log(`Wrote ${shots.length} screenshots to docs/screenshots: ${shots.join(', ')}`);
