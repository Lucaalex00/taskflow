#!/usr/bin/env node
// Copies the project's own docs into public/docs/ so the in-app docs panel
// (see shared/docs-panel) can fetch them as plain static assets — no backend
// endpoint needed. Runs automatically before `npm start` and `npm run build`
// (see package.json's pre* hooks). In Docker, Dockerfile.frontend does the
// same copy explicitly since the build context there doesn't have the
// repo root one level up from frontend/.
const fs = require('fs');
const path = require('path');

const REPO_ROOT = path.resolve(__dirname, '..', '..');
const DEST_DIR = path.resolve(__dirname, '..', 'public', 'docs');

const DOCS = [
  { source: 'README.md', dest: 'readme.md' },
  { source: 'OVERVIEW.md', dest: 'overview.md' }
];

fs.mkdirSync(DEST_DIR, { recursive: true });

for (const { source, dest } of DOCS) {
  const sourcePath = path.join(REPO_ROOT, source);
  if (!fs.existsSync(sourcePath)) {
    console.warn(`copy-docs: ${source} not found at ${sourcePath}, skipping.`);
    continue;
  }
  fs.copyFileSync(sourcePath, path.join(DEST_DIR, dest));
}
