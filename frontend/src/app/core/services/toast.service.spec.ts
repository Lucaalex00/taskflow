import { fakeAsync, tick } from '@angular/core/testing';
import { ToastService } from './toast.service';

describe('ToastService', () => {
  it('success/error/info push a toast of the right type', () => {
    const service = new ToastService();

    service.success('saved');
    service.error('failed');
    service.info('heads up');

    const toasts = service.toasts();
    expect(toasts.map((t) => t.type)).toEqual(['success', 'error', 'info']);
    expect(toasts.map((t) => t.message)).toEqual(['saved', 'failed', 'heads up']);
  });

  it('assigns each toast a unique id', () => {
    const service = new ToastService();
    service.success('a');
    service.success('b');

    const [first, second] = service.toasts();
    expect(first.id).not.toBe(second.id);
  });

  it('dismiss removes a toast by id', () => {
    const service = new ToastService();
    service.success('a');
    const id = service.toasts()[0].id;

    service.dismiss(id);

    expect(service.toasts()).toEqual([]);
  });

  it('auto-dismisses a toast after its duration', fakeAsync(() => {
    const service = new ToastService();
    service.success('temporary');
    expect(service.toasts().length).toBe(1);

    tick(3500);
    expect(service.toasts()).toEqual([]);
  }));
});
