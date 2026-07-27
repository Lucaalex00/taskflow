import { TestBed } from '@angular/core/testing';
import { AvatarComponent } from './avatar.component';

describe('AvatarComponent', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({ imports: [AvatarComponent] });
  });

  function createComponent(name: string | null, color: string | null = '#a855f7') {
    const fixture = TestBed.createComponent(AvatarComponent);
    fixture.componentRef.setInput('name', name);
    fixture.componentRef.setInput('color', color);
    fixture.detectChanges();
    return fixture;
  }

  it('shows the uppercased first initial of the name', () => {
    const fixture = createComponent('ada lovelace');
    expect(fixture.componentInstance.initial()).toBe('A');
    expect((fixture.nativeElement.textContent as string).trim()).toBe('A');
  });

  it('falls back to "?" when the name is empty', () => {
    expect(createComponent('').componentInstance.initial()).toBe('?');
  });

  it('falls back to "?" when the name is null', () => {
    expect(createComponent(null).componentInstance.initial()).toBe('?');
  });

  it('falls back to "?" when the name is whitespace', () => {
    expect(createComponent('   ').componentInstance.initial()).toBe('?');
  });

  it('applies the given color as the background', () => {
    const fixture = createComponent('Ada', '#ec4899');
    const el = fixture.nativeElement.querySelector('.avatar') as HTMLElement;
    // Browsers normalize the hex to rgb.
    expect(el.style.background).toContain('rgb(236, 72, 153)');
  });
});
