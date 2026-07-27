import { Component } from '@angular/core';
import { RouterOutlet, RouterLink } from '@angular/router';
import { CurrentUserService } from './core/services/current-user.service';
import { NotificationBellComponent } from './features/notifications/notification-bell/notification-bell.component';
import { UserMenuComponent } from './shared/user-menu/user-menu.component';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, RouterLink, NotificationBellComponent, UserMenuComponent],
  templateUrl: './app.component.html',
  styleUrl: './app.component.scss'
})
export class AppComponent {
  constructor(readonly currentUser: CurrentUserService) {}
}
