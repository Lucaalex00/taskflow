import { Component } from '@angular/core';
import { ToastService } from '../../core/services/toast.service';

/** Renders the stack of active toasts (top-right). Mounted once in the app shell. */
@Component({
  selector: 'app-toast-container',
  standalone: true,
  templateUrl: './toast-container.component.html',
  styleUrl: './toast-container.component.scss'
})
export class ToastContainerComponent {
  constructor(readonly toastService: ToastService) {}

  icon(type: string): string {
    return type === 'success' ? '✓' : type === 'error' ? '✕' : 'ℹ';
  }
}
