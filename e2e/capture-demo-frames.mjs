/**
 * Captures the frames behind the animated demo in README.md, driving the real app exactly the
 * way the E2E suite does. Companion to capture-screenshots.mjs; the frames are assembled into
 * a GIF by build-demo-gif.py (PIL) — deliberately no ffmpeg dependency.
 *
 * Requires the stack running with the demo workspace seeded (the default):
 *   docker compose up --build -d
 *   cd e2e && node capture-demo-frames.mjs && python build-demo-gif.py
 */
import { chromium } from '@playwright/test';
import { mkdir, rm } from 'node:fs/promises';
import { fileURLToPath } from 'node:url';
import { join } from 'node:path';

const BASE_URL = process.env.E2E_BASE_URL ?? 'http://localhost:4200';
const FRAME_DIR = fileURLToPath(new URL('./.demo-frames/', import.meta.url));

/** Grabs frames on a timer while the scripted interaction runs, so the GIF shows real motion
 * (a card actually travelling between columns) rather than a series of end states. */
class FrameRecorder {
  constructor(page) {
    this.page = page;
    this.index = 0;
    this.running = false;
  }

  start(intervalMs = 110) {
    this.running = true;
    this.loop = (async () => {
      while (this.running) {
        const started = Date.now();
        await this.page
          .screenshot({ path: join(FRAME_DIR, `f${String(this.index++).padStart(4, '0')}.png`) })
          .catch(() => {}); // a frame landing mid-navigation is not worth failing the run over
        const remaining = intervalMs - (Date.now() - started);
        if (remaining > 0) await this.page.waitForTimeout(remaining);
      }
    })();
  }

  async stop() {
    this.running = false;
    await this.loop;
    return this.index;
  }

  /** Holds on the current view so a viewer's eye can land on what just happened. */
  hold(ms) {
    return this.page.waitForTimeout(ms);
  }
}

/** Angular CDK only starts a drag once the pointer has moved a few pixels, so nudge first —
 * same approach as tests/dragdrop.spec.ts. Extra steps here are for a smooth-looking GIF. */
async function dragCardToColumn(page, cardTitle, columnState) {
  const card = page.locator('.task-card', { hasText: cardTitle });
  const target = page.locator(`#column-${columnState}`);
  const cb = await card.boundingBox();
  const tb = await target.boundingBox();
  if (!cb || !tb) throw new Error(`card "${cardTitle}" or column ${columnState} not found`);

  await page.mouse.move(cb.x + cb.width / 2, cb.y + 20);
  await page.mouse.down();
  await page.mouse.move(cb.x + cb.width / 2 + 10, cb.y + 30, { steps: 5 });
  await page.mouse.move(tb.x + tb.width / 2, tb.y + 120, { steps: 28 });
  await page.mouse.move(tb.x + tb.width / 2, tb.y + 140, { steps: 6 });
  await page.mouse.up();
}

await rm(FRAME_DIR, { recursive: true, force: true });
await mkdir(FRAME_DIR, { recursive: true });

const browser = await chromium.launch();
const page = await browser.newPage({ viewport: { width: 1280, height: 760 }, colorScheme: 'dark' });

await page.goto(BASE_URL);
await page.getByRole('button', { name: 'Explore the demo workspace' }).waitFor();

const recorder = new FrameRecorder(page);
recorder.start();

await recorder.hold(900); // the login screen, with the demo button
await page.getByRole('button', { name: 'Explore the demo workspace' }).click();
await page.getByText('Product launch').waitFor();
await recorder.hold(1100); // the board list

await page.getByText('Product launch').click();
await page.locator('.task-card').first().waitFor();
await recorder.hold(1600); // the Kanban, with the alert console already populated

await dragCardToColumn(page, 'Write the launch-day runbook', 'InProgress');
await recorder.hold(1600); // the card has landed, the toast confirms

await page.getByRole('button', { name: /Switch to light theme/i }).click();
await recorder.hold(1500);
await page.getByRole('button', { name: /Switch to dark theme/i }).click();
await recorder.hold(900);

const frames = await recorder.stop();
await browser.close();

console.log(`Captured ${frames} frames into e2e/.demo-frames — now run: python build-demo-gif.py`);
