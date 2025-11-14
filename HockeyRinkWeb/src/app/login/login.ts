import { Component, signal } from "@angular/core";
import { Router } from "@angular/router";
import { AuthService } from "../auth";
import {
  ReactiveFormsModule,
  FormBuilder,
  FormGroup,
  Validators,
} from "@angular/forms";
import { CommonModule } from "@angular/common";
import { ToastService } from "../services/toast.service";

@Component({
  selector: "app-login",
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: "./login.html",
  styleUrl: "./login.css",
})
export class Login {
  loginForm: FormGroup;
  errorMessage = signal<string>("");
  isLoading = signal<boolean>(false);

  constructor(
    private authService: AuthService,
    private router: Router,
    private fb: FormBuilder,
    private toastService: ToastService
  ) {
    this.loginForm = this.fb.group({
      email: ["", [Validators.required, Validators.email]],
      password: ["", [Validators.required, Validators.minLength(8)]],
    });
  }

  hasError(field: string, errorType: string): boolean {
    const control = this.loginForm.get(field);
    return !!(
      control &&
      control.hasError(errorType) &&
      (control.dirty || control.touched)
    );
  }

  onLogin() {
    this.errorMessage.set("");

    if (this.loginForm.invalid) {
      this.loginForm.markAllAsTouched();
      return;
    }

    this.isLoading.set(true);
    const { email, password } = this.loginForm.value;

    this.authService.login(email, password).subscribe({
      next: () => {
        console.log("Login successful");
        this.isLoading.set(false);
        this.toastService.success("Login Successful", "Welcome back!");
        this.router.navigate(["/dashboard"]);
      },
      error: (err) => {
        console.error("Login failed:", err);
        this.isLoading.set(false);
        const message =
          err.error?.message || "Invalid email or password. Please try again.";
        this.errorMessage.set(message);
        this.toastService.error("Login Failed", message);
      },
    });
  }
}
