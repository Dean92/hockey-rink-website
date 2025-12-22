import { Component, OnInit, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { CommonModule } from '@angular/common';
import {
  ReactiveFormsModule,
  FormBuilder,
  FormGroup,
  Validators,
} from '@angular/forms';
import { DataService } from '../data';

@Component({
  selector: 'app-setup-password',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './setup-password.html',
  styleUrl: './setup-password.css',
})
export class SetupPassword implements OnInit {
  setupForm: FormGroup;
  token = signal<string>('');
  userEmail = signal<string>('');
  userName = signal<string>('');
  isLoading = signal<boolean>(true);
  isSubmitting = signal<boolean>(false);
  errorMessage = signal<string | null>(null);
  successMessage = signal<string | null>(null);
  tokenValid = signal<boolean>(false);

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private formBuilder: FormBuilder,
    private dataService: DataService
  ) {
    this.setupForm = this.formBuilder.group(
      {
        password: ['', [Validators.required, Validators.minLength(6)]],
        confirmPassword: ['', Validators.required],
      },
      { validators: this.passwordMatchValidator }
    );
  }

  ngOnInit() {
    this.route.params.subscribe((params) => {
      const token = params['token'];
      if (token) {
        this.token.set(token);
        this.validateToken(token);
      } else {
        this.errorMessage.set('Invalid password setup link');
        this.isLoading.set(false);
      }
    });
  }

  validateToken(token: string) {
    this.dataService.validatePasswordSetupToken(token).subscribe({
      next: (response: any) => {
        this.userEmail.set(response.email);
        this.userName.set(`${response.firstName} ${response.lastName}`);
        this.tokenValid.set(true);
        this.isLoading.set(false);
      },
      error: (err) => {
        this.errorMessage.set(
          err.error?.message || 'Invalid or expired setup link'
        );
        this.tokenValid.set(false);
        this.isLoading.set(false);
      },
    });
  }

  passwordMatchValidator(formGroup: FormGroup) {
    const password = formGroup.get('password')?.value;
    const confirmPassword = formGroup.get('confirmPassword')?.value;
    return password === confirmPassword ? null : { passwordMismatch: true };
  }

  onSubmit() {
    if (this.setupForm.invalid) {
      this.setupForm.markAllAsTouched();
      return;
    }

    this.isSubmitting.set(true);
    this.errorMessage.set(null);

    const setupData = {
      token: this.token(),
      password: this.setupForm.value.password,
      confirmPassword: this.setupForm.value.confirmPassword,
    };

    this.dataService.setupPassword(setupData).subscribe({
      next: (response: any) => {
        this.successMessage.set('Password setup successful! Redirecting...');

        // Store token and redirect to dashboard
        if (response.token) {
          localStorage.setItem('token', response.token);
        }

        setTimeout(() => {
          this.router.navigate(['/dashboard']);
        }, 2000);
      },
      error: (err) => {
        this.errorMessage.set(
          err.error?.message || 'Failed to setup password. Please try again.'
        );
        this.isSubmitting.set(false);
      },
    });
  }

  hasError(field: string, errorType: string): boolean {
    const control = this.setupForm.get(field);
    return !!(
      control &&
      control.hasError(errorType) &&
      (control.dirty || control.touched)
    );
  }

  get passwordMismatch(): boolean {
    return (
      this.setupForm.hasError('passwordMismatch') &&
      this.setupForm.get('confirmPassword')?.touched === true
    );
  }
}
