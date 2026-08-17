import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { LoginComponent } from './login.component';
import { CurrentUserService } from '../../../core/services/current-user.service';
import { AppConfigService } from '../../../core/services/app-config.service';

describe('LoginComponent', () => {
  let currentUser: jasmine.SpyObj<CurrentUserService>;
  let router: jasmine.SpyObj<Router>;
  let appConfig: jasmine.SpyObj<AppConfigService>;

  function createComponent() {
    currentUser = jasmine.createSpyObj<CurrentUserService>('CurrentUserService', ['login', 'register']);
    router = jasmine.createSpyObj<Router>('Router', ['navigateByUrl']);
    router.navigateByUrl.and.resolveTo(true);
    appConfig = jasmine.createSpyObj<AppConfigService>('AppConfigService', ['load']);
    appConfig.load.and.resolveTo({ demoAccountAvailable: false, demoEmail: null, demoPassword: null });

    TestBed.configureTestingModule({
      imports: [LoginComponent],
      providers: [
        { provide: CurrentUserService, useValue: currentUser },
        { provide: Router, useValue: router },
        { provide: AppConfigService, useValue: appConfig }
      ]
    });

    const fixture = TestBed.createComponent(LoginComponent);
    return { fixture, component: fixture.componentInstance };
  }

  it('defaults to login mode', () => {
    const { component } = createComponent();

    expect(component.mode()).toBe('login');
  });

  it('switchMode changes the mode and clears any error', () => {
    const { component } = createComponent();
    component.errorMessage.set('boom');

    component.switchMode('register');

    expect(component.mode()).toBe('register');
    expect(component.errorMessage()).toBeNull();
  });

  it('submit logs in and navigates home on success (login mode)', async () => {
    const { component } = createComponent();
    currentUser.login.and.resolveTo(undefined);
    component.email = 'ada@example.com';
    component.password.set('password123');

    await component.submit();

    expect(currentUser.login).toHaveBeenCalledWith({ email: 'ada@example.com', password: 'password123' });
    expect(router.navigateByUrl).toHaveBeenCalledWith('/');
  });

  it('submit registers and navigates home on success (register mode)', async () => {
    const { component } = createComponent();
    currentUser.register.and.resolveTo(undefined);
    component.switchMode('register');
    component.email = 'ada@example.com';
    component.displayName = 'Ada';
    component.password.set('Password123');

    await component.submit();

    expect(currentUser.register).toHaveBeenCalledWith({
      email: 'ada@example.com',
      displayName: 'Ada',
      password: 'Password123'
    });
    expect(router.navigateByUrl).toHaveBeenCalledWith('/');
  });

  it('submit sets an error message when login fails', async () => {
    const { component } = createComponent();
    currentUser.login.and.rejectWith(new Error('boom'));
    component.email = 'ada@example.com';
    component.password.set('wrong-password');

    await component.submit();

    expect(component.errorMessage()).toBe('Invalid email or password.');
    expect(component.isSubmitting()).toBeFalse();
  });

  it('submit does nothing when required fields are blank', async () => {
    const { component } = createComponent();
    component.email = '';
    component.password.set('');

    await component.submit();

    expect(currentUser.login).not.toHaveBeenCalled();
  });

  it('submit does nothing in register mode when the display name is blank', async () => {
    const { component } = createComponent();
    component.switchMode('register');
    component.email = 'ada@example.com';
    component.password.set('Password123');
    component.displayName = '   ';

    await component.submit();

    expect(currentUser.register).not.toHaveBeenCalled();
  });

  it('isPasswordValid reflects the live password policy (register mode checklist)', () => {
    const { component } = createComponent();

    component.password.set('short');
    expect(component.isPasswordValid()).toBeFalse();
    expect(component.passwordRequirements().every((r) => r.met)).toBeFalse();

    component.password.set('Password123');
    expect(component.isPasswordValid()).toBeTrue();
    expect(component.passwordRequirements().every((r) => r.met)).toBeTrue();
  });

  it('submit does nothing in register mode when the password fails the policy', async () => {
    const { component } = createComponent();
    component.switchMode('register');
    component.email = 'ada@example.com';
    component.displayName = 'Ada';
    component.password.set('weak'); // too short, no uppercase, no number

    await component.submit();

    expect(currentUser.register).not.toHaveBeenCalled();
  });

  it('offers the demo account when the instance seeded one', async () => {
    const { component } = createComponent();
    appConfig.load.and.resolveTo({
      demoAccountAvailable: true,
      demoEmail: 'demo@taskflow.dev',
      demoPassword: 'Demo-password-2026'
    });

    await component.ngOnInit();

    expect(component.demoAccount()?.demoEmail).toBe('demo@taskflow.dev');
  });

  it('hides the demo account on an instance without one', async () => {
    const { component } = createComponent();

    await component.ngOnInit();

    expect(component.demoAccount()).toBeNull();
  });

  it('signInAsDemo logs in with the advertised credentials and navigates home', async () => {
    const { component } = createComponent();
    currentUser.login.and.resolveTo(undefined);
    component.demoAccount.set({
      demoAccountAvailable: true,
      demoEmail: 'demo@taskflow.dev',
      demoPassword: 'Demo-password-2026'
    });

    await component.signInAsDemo();

    expect(currentUser.login).toHaveBeenCalledWith({
      email: 'demo@taskflow.dev',
      password: 'Demo-password-2026'
    });
    expect(router.navigateByUrl).toHaveBeenCalledWith('/');
  });

  it('signInAsDemo surfaces an error instead of navigating when the demo login fails', async () => {
    const { component } = createComponent();
    currentUser.login.and.rejectWith(new Error('nope'));
    component.demoAccount.set({
      demoAccountAvailable: true,
      demoEmail: 'demo@taskflow.dev',
      demoPassword: 'Demo-password-2026'
    });

    await component.signInAsDemo();

    expect(router.navigateByUrl).not.toHaveBeenCalled();
    expect(component.errorMessage()).toContain('demo account is unavailable');
  });
});
