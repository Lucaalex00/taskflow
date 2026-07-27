import { Component, computed, input } from '@angular/core';

/** A small circular avatar showing a person's first initial on their chosen color. Used
 * wherever a user is referenced (header, member list, task assignee). */
@Component({
  selector: 'app-avatar',
  standalone: true,
  template: `
    <span
      class="avatar"
      [class.avatar--sm]="size() === 'sm'"
      [class.avatar--lg]="size() === 'lg'"
      [style.background]="color()"
      [attr.title]="name()"
      [attr.aria-label]="name()"
    >{{ initial() }}</span>
  `,
  styleUrl: './avatar.component.scss'
})
export class AvatarComponent {
  readonly name = input<string | null>('');
  readonly color = input<string | null>('#a855f7');
  readonly size = input<'sm' | 'md' | 'lg'>('md');

  readonly initial = computed(() => {
    const n = (this.name() ?? '').trim();
    return n ? n.charAt(0).toUpperCase() : '?';
  });
}
