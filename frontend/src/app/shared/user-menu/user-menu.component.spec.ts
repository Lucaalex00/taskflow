import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { signal } from '@angular/core';
import { UserMenuComponent } from './user-menu.component';
import { CurrentUserService } from '../../core/services/current-user.service';

describe('UserMenuComponent', () => {
  let currentUser: {
    displayName: ReturnType<typeof signal<string | null>>;
    color: ReturnType<typeof signal<string | null>>;
    updateColor: jasmine.Spy;
    signOut: jasmine.Spy;
  };
  let router: jasmine.SpyObj<Router>;

  function createComponent() {
    currentUser = {
      displayName: signal<string | null>('Ada'),
      color: signal<string | null>('#a855f7'),
      updateColor: jasmine.createSpy('updateColor').and.resolveTo(undefined),
      signOut: jasmine.createSpy('signOut')
    };
    router = jasmine.createSpyObj<Router>('Router', ['navigateByUrl']);

    TestBed.configureTestingModule({
      imports: [UserMenuComponent],
      providers: [
        { provide: CurrentUserService, useValue: currentUser },
        { provide: Router, useValue: router }
      ]
    });
    const fixture = TestBed.createComponent(UserMenuComponent);
    return { fixture, component: fixture.componentInstance };
  }

  it('toggle opens and closes the menu', () => {
    const { component } = createComponent();

    component.toggle();
    expect(component.isOpen()).toBeTrue();
    component.toggle();
    expect(component.isOpen()).toBeFalse();
  });

  it('onColorInput saves the picked color through the service', async () => {
    const { component } = createComponent();
    const event = { target: { value: '#ec4899' } } as unknown as Event;

    await component.onColorInput(event);

    expect(currentUser.updateColor).toHaveBeenCalledWith('#ec4899');
  });

  it('onColorInput surfaces an error message on failure', async () => {
    const { component } = createComponent();
    currentUser.updateColor.and.rejectWith(new Error('boom'));

    await component.onColorInput({ target: { value: '#000000' } } as unknown as Event);

    expect(component.errorMessage()).toContain('Could not save');
  });

  it('signOut clears the session and redirects to /login', () => {
    const { component } = createComponent();

    component.signOut();

    expect(currentUser.signOut).toHaveBeenCalled();
    expect(router.navigateByUrl).toHaveBeenCalledWith('/login');
  });
});
