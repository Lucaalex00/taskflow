import { TestBed } from '@angular/core/testing';
import { HttpErrorResponse } from '@angular/common/http';
import { signal } from '@angular/core';
import { provideRouter } from '@angular/router';
import { ProfileComponent } from './profile.component';
import { CurrentUserService } from '../../core/services/current-user.service';
import { ThemeService } from '../../core/services/theme.service';
import { ToastService } from '../../core/services/toast.service';

describe('ProfileComponent', () => {
  let currentUser: jasmine.SpyObj<CurrentUserService>;
  let theme: jasmine.SpyObj<ThemeService>;
  let toasts: jasmine.SpyObj<ToastService>;

  function createComponent() {
    currentUser = jasmine.createSpyObj<CurrentUserService>(
      'CurrentUserService',
      ['updateDisplayName', 'changePassword', 'updateColor'],
      { displayName: signal('Ada'), color: signal('#4fd1c5') }
    );
    theme = jasmine.createSpyObj<ThemeService>('ThemeService', ['set', 'toggle'], { theme: signal('dark') });
    toasts = jasmine.createSpyObj<ToastService>('ToastService', ['success', 'error', 'info']);

    TestBed.configureTestingModule({
      imports: [ProfileComponent],
      providers: [
        provideRouter([]),
        { provide: CurrentUserService, useValue: currentUser },
        { provide: ThemeService, useValue: theme },
        { provide: ToastService, useValue: toasts }
      ]
    });

    const fixture = TestBed.createComponent(ProfileComponent);
    return { fixture, component: fixture.componentInstance };
  }

  it('pre-fills the form with the signed-in user’s current name', () => {
    const { component } = createComponent();

    expect(component.displayName).toBe('Ada');
  });

  it('saveProfile renames the user and confirms with a toast', async () => {
    const { component } = createComponent();
    currentUser.updateDisplayName.and.resolveTo(undefined);
    component.displayName = '  Ada Lovelace  ';

    await component.saveProfile();

    expect(currentUser.updateDisplayName).toHaveBeenCalledWith('Ada Lovelace');
    expect(toasts.success).toHaveBeenCalled();
    expect(component.profileError()).toBeNull();
  });

  it('saveProfile does nothing when the name is blank', async () => {
    const { component } = createComponent();
    component.displayName = '   ';

    await component.saveProfile();

    expect(currentUser.updateDisplayName).not.toHaveBeenCalled();
  });

  it('changePassword clears both fields and confirms on success', async () => {
    const { component } = createComponent();
    currentUser.changePassword.and.resolveTo(undefined);
    component.currentPassword = 'Old-password-1';
    component.newPassword.set('New-password-2');

    await component.changePassword();

    expect(currentUser.changePassword).toHaveBeenCalledWith('Old-password-1', 'New-password-2');
    expect(component.currentPassword).toBe('');
    expect(component.newPassword()).toBe('');
    expect(toasts.success).toHaveBeenCalled();
  });

  it('changePassword refuses a new password that fails the policy', async () => {
    const { component } = createComponent();
    component.currentPassword = 'Old-password-1';
    component.newPassword.set('weak');

    await component.changePassword();

    expect(component.isNewPasswordValid()).toBeFalse();
    expect(currentUser.changePassword).not.toHaveBeenCalled();
  });

  it('changePassword surfaces the server’s per-field message', async () => {
    const { component } = createComponent();
    currentUser.changePassword.and.rejectWith(
      new HttpErrorResponse({
        status: 400,
        error: { detail: 'One or more validation failures occurred.', errors: { CurrentPassword: ['Current password is incorrect.'] } }
      })
    );
    component.currentPassword = 'Wrong-password-1';
    component.newPassword.set('New-password-2');

    await component.changePassword();

    expect(component.passwordError()).toBe('Current password is incorrect.');
    expect(component.currentPassword).toBe('Wrong-password-1');
  });

  it('selectTheme delegates to the theme service', () => {
    const { component } = createComponent();

    component.selectTheme('light');

    expect(theme.set).toHaveBeenCalledWith('light');
  });
});
