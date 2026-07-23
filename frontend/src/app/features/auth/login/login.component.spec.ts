import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { LoginComponent } from './login.component';
import { CurrentUserService } from '../../../core/services/current-user.service';

describe('LoginComponent', () => {
  let currentUser: jasmine.SpyObj<CurrentUserService>;
  let router: jasmine.SpyObj<Router>;

  function createComponent() {
    currentUser = jasmine.createSpyObj<CurrentUserService>('CurrentUserService', ['login', 'register']);
    router = jasmine.createSpyObj<Router>('Router', ['navigateByUrl']);
    router.navigateByUrl.and.resolveTo(true);

    TestBed.configureTestingModule({
      imports: [LoginComponent],
      providers: [
        { provide: CurrentUserService, useValue: currentUser },
        { provide: Router, useValue: router }
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
    component.password = 'password123';

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
    component.password = 'password123';

    await component.submit();

    expect(currentUser.register).toHaveBeenCalledWith({
      email: 'ada@example.com',
      displayName: 'Ada',
      password: 'password123'
    });
    expect(router.navigateByUrl).toHaveBeenCalledWith('/');
  });

  it('submit sets an error message when login fails', async () => {
    const { component } = createComponent();
    currentUser.login.and.rejectWith(new Error('boom'));
    component.email = 'ada@example.com';
    component.password = 'wrong-password';

    await component.submit();

    expect(component.errorMessage()).toBe('Invalid email or password.');
    expect(component.isSubmitting()).toBeFalse();
  });

  it('submit does nothing when required fields are blank', async () => {
    const { component } = createComponent();
    component.email = '';
    component.password = '';

    await component.submit();

    expect(currentUser.login).not.toHaveBeenCalled();
  });

  it('submit does nothing in register mode when the display name is blank', async () => {
    const { component } = createComponent();
    component.switchMode('register');
    component.email = 'ada@example.com';
    component.password = 'password123';
    component.displayName = '   ';

    await component.submit();

    expect(currentUser.register).not.toHaveBeenCalled();
  });
});
