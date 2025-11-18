import { Component, inject } from "@angular/core";
import { CommonModule } from "@angular/common";
import { ToastService } from "../services/toast.service";

@Component({
  selector: "app-toast-container",
  standalone: true,
  imports: [CommonModule],
  templateUrl: "./toast-container.component.html",
  styleUrls: ["./toast-container.component.css"],
})
export class ToastContainerComponent {
  protected toastService = inject(ToastService);

  getIconClass(type: string): string {
    switch (type) {
      case "success":
        return "bi-check-circle-fill";
      case "error":
        return "bi-x-circle-fill";
      case "warning":
        return "bi-exclamation-triangle-fill";
      case "info":
        return "bi-info-circle-fill";
      default:
        return "bi-info-circle-fill";
    }
  }

  getBgClass(type: string): string {
    switch (type) {
      case "success":
        return "bg-success";
      case "error":
        return "bg-danger";
      case "warning":
        return "bg-warning";
      case "info":
        return "bg-info";
      default:
        return "bg-primary";
    }
  }
}
