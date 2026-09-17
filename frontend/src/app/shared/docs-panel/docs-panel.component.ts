import { Component, HostListener, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { DomSanitizer, SafeHtml } from '@angular/platform-browser';
import { Marked, RendererObject, Tokens } from 'marked';
import { firstValueFrom } from 'rxjs';

interface DocTab {
  readonly id: string;
  readonly label: string;
  readonly file: string;
}

const TABS: readonly DocTab[] = [
  { id: 'readme', label: 'README', file: 'docs/readme.md' },
  { id: 'overview', label: 'How it works', file: 'docs/overview.md' }
];

const GITHUB_REPO_URL = 'https://github.com/Lucaalex00/taskflow';
const GITHUB_BLOB_URL = `${GITHUB_REPO_URL}/blob/main/`;
const GITHUB_RAW_URL = 'https://raw.githubusercontent.com/Lucaalex00/taskflow/main/';

/** True for a path relative to the repo root, e.g. "docs/screenshots/demo.gif" — false for an
 * absolute URL (http/https/mailto/...) or an in-page anchor ("#configuration"). */
function isRepoRelative(href: string): boolean {
  return !/^[a-z][a-z0-9+.-]*:/i.test(href) && !href.startsWith('#');
}

/**
 * The README/OVERVIEW are written to be browsed on GitHub, so their relative image/link paths
 * (e.g. "docs/screenshots/demo.gif") are relative to the repo root — not to wherever this panel
 * happens to be mounted in the app. Rewriting them to absolute GitHub URLs is what keeps them
 * resolving correctly here, the same fix File_Analyzer's own docs viewer uses.
 */
const docsRenderer: RendererObject = {
  image({ href, title, text }: Tokens.Image): string {
    const resolvedHref = isRepoRelative(href) ? GITHUB_RAW_URL + href.replace(/^\.?\//, '') : href;
    const titleAttr = title ? ` title="${title}"` : '';
    return `<img src="${resolvedHref}" alt="${text}"${titleAttr}>`;
  },
  link({ href, title, tokens }: Tokens.Link): string {
    const text = this.parser.parseInline(tokens);
    const resolvedHref = isRepoRelative(href) ? GITHUB_BLOB_URL + href.replace(/^\.?\//, '') : href;
    const titleAttr = title ? ` title="${title}"` : '';
    return `<a href="${resolvedHref}" target="_blank" rel="noopener noreferrer"${titleAttr}>${text}</a>`;
  }
};

/** Isolated instance (not the global `marked` singleton) so this renderer never leaks into any
 * other markdown rendering the app might add later. */
const docsMarked = new Marked({ renderer: docsRenderer });

/**
 * Slide-over panel that renders the project's own README/OVERVIEW inside the app, so a visitor
 * doesn't have to leave for GitHub to see how it's built. The markdown files are copied into
 * public/docs/ at build time (see scripts/copy-docs.js and Dockerfile.frontend) and fetched here
 * as plain static assets — content is our own, never visitor-supplied, so it's safe to render
 * via bypassSecurityTrustHtml rather than needing a sanitizing markdown pipeline.
 */
@Component({
  selector: 'app-docs-panel',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './docs-panel.component.html',
  styleUrl: './docs-panel.component.scss'
})
export class DocsPanelComponent {
  readonly tabs = TABS;
  readonly repoUrl = GITHUB_REPO_URL;

  readonly isOpen = signal(false);
  readonly activeTabId = signal<string>(TABS[0].id);
  readonly isLoading = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly content = signal<SafeHtml | null>(null);

  private readonly cache = new Map<string, SafeHtml>();

  constructor(
    private readonly http: HttpClient,
    private readonly sanitizer: DomSanitizer
  ) {}

  open(): void {
    this.isOpen.set(true);
    void this.loadTab(this.activeTabId());
  }

  close(): void {
    this.isOpen.set(false);
  }

  selectTab(tabId: string): void {
    this.activeTabId.set(tabId);
    void this.loadTab(tabId);
  }

  @HostListener('document:keydown.escape')
  onEscape(): void {
    if (this.isOpen()) this.close();
  }

  private async loadTab(tabId: string): Promise<void> {
    const cached = this.cache.get(tabId);
    if (cached) {
      this.content.set(cached);
      this.errorMessage.set(null);
      return;
    }

    const tab = this.tabs.find((t) => t.id === tabId);
    if (!tab) return;

    this.isLoading.set(true);
    this.errorMessage.set(null);

    try {
      const markdown = await firstValueFrom(this.http.get(tab.file, { responseType: 'text' }));
      const html = this.sanitizer.bypassSecurityTrustHtml(await docsMarked.parse(markdown));
      this.cache.set(tabId, html);
      this.content.set(html);
    } catch {
      this.errorMessage.set('Could not load this document.');
    } finally {
      this.isLoading.set(false);
    }
  }
}
