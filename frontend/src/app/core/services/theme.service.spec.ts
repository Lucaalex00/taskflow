import { TestBed } from '@angular/core/testing';
import { ThemeService } from './theme.service';

describe('ThemeService', () => {
  function createService(): ThemeService {
    TestBed.configureTestingModule({});
    return TestBed.inject(ThemeService);
  }

  beforeEach(() => {
    localStorage.clear();
    document.documentElement.removeAttribute('data-theme');
  });

  afterEach(() => {
    localStorage.clear();
    document.documentElement.removeAttribute('data-theme');
  });

  it('restores a previously chosen theme and applies it to the document', () => {
    localStorage.setItem('taskflow.theme', 'light');

    const service = createService();

    expect(service.theme()).toBe('light');
    expect(document.documentElement.getAttribute('data-theme')).toBe('light');
  });

  it('ignores a stored value that is not a known theme', () => {
    localStorage.setItem('taskflow.theme', 'neon');

    const service = createService();

    expect(['dark', 'light']).toContain(service.theme());
  });

  it('toggle flips the theme, persists it and updates the document attribute', () => {
    const service = createService();
    const initial = service.theme();
    const expected = initial === 'dark' ? 'light' : 'dark';

    service.toggle();

    expect(service.theme()).toBe(expected);
    expect(localStorage.getItem('taskflow.theme')).toBe(expected);
    expect(document.documentElement.getAttribute('data-theme')).toBe(expected);
  });

  it('set applies an explicit choice', () => {
    const service = createService();

    service.set('light');

    expect(service.theme()).toBe('light');
    expect(document.documentElement.getAttribute('data-theme')).toBe('light');

    service.set('dark');

    expect(service.theme()).toBe('dark');
    expect(document.documentElement.getAttribute('data-theme')).toBe('dark');
  });
});
