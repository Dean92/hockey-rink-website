import { Injectable, signal } from "@angular/core";

export interface Toast {
  id: number;
  type: "success" | "error" | "info" | "warning";
  title: string;
  message: string;
  timestamp: Date;
}

@Injectable({
  providedIn: "root",
})
export class ToastService {
  private nextId = 1;
  public toasts = signal<Toast[]>([]);

  private addToast(type: Toast["type"], title: string, message: string): void {
    const toast: Toast = {
      id: this.nextId++,
      type,
      title,
      message,
      timestamp: new Date(),
    };

    this.toasts.update((toasts) => [...toasts, toast]);

    // Auto-remove after 5 seconds
    setTimeout(() => this.remove(toast.id), 5000);
  }

  success(title: string, message: string): void {
    this.addToast("success", title, message);
  }

  error(title: string, message: string): void {
    this.addToast("error", title, message);
  }

  info(title: string, message: string): void {
    this.addToast("info", title, message);
  }

  warning(title: string, message: string): void {
    this.addToast("warning", title, message);
  }

  remove(id: number): void {
    this.toasts.update((toasts) => toasts.filter((t) => t.id !== id));
  }
}
