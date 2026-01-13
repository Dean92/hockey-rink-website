import { Component, signal } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from '../auth';
import {
  ReactiveFormsModule,
  FormBuilder,
  FormGroup,
  Validators,
} from '@angular/forms';
import { CommonModule } from '@angular/common';
import { ToastService } from '../services/toast.service';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './register.html',
  styleUrl: './register.css',
})
export class Register {
  registerForm: FormGroup;
  errorMessage = signal<string>('');
  isLoading = signal<boolean>(false);

  constructor(
    private authService: AuthService,
    private router: Router,
    private fb: FormBuilder,
    private toastService: ToastService
  ) {
    this.registerForm = this.fb.group({
      firstName: ['', [Validators.required]],
      lastName: ['', [Validators.required]],
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required, Validators.minLength(8)]],
    });
  }

  hasError(field: string, errorType: string): boolean {
    const control = this.registerForm.get(field);
    return !!(
      control &&
      control.hasError(errorType) &&
      (control.dirty || control.touched)
    );
  }

  onRegister() {
    this.errorMessage.set('');

    if (this.registerForm.invalid) {
      this.registerForm.markAllAsTouched();
      return;
    }

    this.isLoading.set(true);
    const { firstName, lastName, email, password } = this.registerForm.value;

    this.authService.register(firstName, lastName, email, password).subscribe({
      next: () => {
        console.log('Registration successful');
        this.isLoading.set(false);
        this.toastService.success(
          'Registration Successful',
          'Welcome! You can now browse leagues.'
        );
        this.router.navigate(['/leagues']);
      },
      error: (err) => {
        console.error('Registration failed:', err);
        this.isLoading.set(false);

        let message = 'Registration failed. Please try again.';

        // Check for ModelState errors (400 BadRequest with validation errors)
        if (err.status === 400 && err.error) {
          if (err.error.errors) {
            // Extract all error messages from ModelState
            const errors = Object.values(err.error.errors).flat().join(' ');
            message = errors || message;
          } else if (typeof err.error === 'object') {
            // Extract errors from empty string key (common in ModelState)
            const emptyKeyErrors = err.error[''] || err.error[''];
            if (Array.isArray(emptyKeyErrors) && emptyKeyErrors.length > 0) {
              message = emptyKeyErrors.join(' ');
            } else if (err.error.message) {
              message = err.error.message;
            }
          }
        } else if (err.error?.message) {
          message = err.error.message;
        } else if (err.error?.details) {
          message = err.error.details;
        }

        this.errorMessage.set(message);
        this.toastService.error('Registration Failed', message);
      },
    });
  }
}
