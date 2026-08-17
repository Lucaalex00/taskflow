import { TestBed } from '@angular/core/testing';
import { signal } from '@angular/core';
import { ThemeToggleComponent } from './theme-toggle.component';
import { ThemeService } from '../../core/services/theme.service';

describe('ThemeToggleComponent', () => {
  function createComponent(current: 'dark' | 'light') {
    const theme = jasmine.createSpyObj<ThemeService>('ThemeService', ['toggle', 'set'], {
      theme: signal(current)
    });

    TestBed.configureTestingModule({
      imports: [ThemeToggleComponent],
      providers: [{ provide: ThemeService, useValue: theme }]
    });

    const fixture = TestBed.createComponent(ThemeToggleComponent);
    fixture.detectChanges();
    return { fixture, component: fixture.componentInstance, theme };
  }

  it('labels the action by where it takes you, not where you are', () => {
    expect(createComponent('dark').component.label()).toBe('Switch to light theme');
    TestBed.resetTestingModule();
    expect(createComponent('light').component.label()).toBe('Switch to dark theme');
  });

  it('clicking it toggles the theme', () => {
    const { fixture, theme } = createComponent('dark');

    fixture.nativeElement.querySelector('button').click();

    expect(theme.toggle).toHaveBeenCalled();
  });
});
