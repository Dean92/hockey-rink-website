import { Component } from "@angular/core";
import { AuthService } from "../auth";
import { Router } from "@angular/router";
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators, AbstractControl, ValidationErrors } from "@angular/forms";
import { CommonModule } from "@angular/common";

@Component({
  selector: "app-register",
  imports: [ReactiveFormsModule, CommonModule],
  templateUrl: "./register.html",
  styleUrls: ["./register.css"],
})
export class Register {
  registerForm: FormGroup;
  errors: string[] = [];

  constructor(
    private authService: AuthService, 
    private router: Router,
    private formBuilder: FormBuilder
  ) {
    this.registerForm = this.formBuilder.group({
      firstName: ['', [Validators.required, Validators.minLength(2)]],
      lastName: ['', [Validators.required, Validators.minLength(2)]],
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required, Validators.minLength(8), this.passwordValidator]]
    });
  }

  // Custom validator for password complexity
  passwordValidator(control: AbstractControl): ValidationErrors | null {
    const value = control.value;
    if (!value) {
      return null; // Let required validator handle empty values
    }

    const hasDigit = /\d/.test(value);
    const hasSpecialChar = /[^a-zA-Z0-9]/.test(value);

    const errors: ValidationErrors = {};
    
    if (!hasDigit) {
      errors['noDigit'] = true;
    }
    
    if (!hasSpecialChar) {
      errors['noSpecialChar'] = true;
    }

    return Object.keys(errors).length > 0 ? errors : null;
  }

  // Helper method to get form control
  getControl(name: string) {
    return this.registerForm.get(name);
  }

  // Helper method to check if a field has a specific error
  hasError(field: string, errorType: string): boolean {
    const control = this.getControl(field);
    return !!(control && control.hasError(errorType) && (control.dirty || control.touched));
  }

  // Get validation error messages for display
  getFieldErrors(field: string): string[] {
    const control = this.getControl(field);
    const errors: string[] = [];

    if (control && control.errors && (control.dirty || control.touched)) {
      if (control.errors['required']) {
        errors.push(`${this.getFieldDisplayName(field)} is required.`);
      }
      if (control.errors['minlength']) {
        const requiredLength = control.errors['minlength'].requiredLength;
        errors.push(`${this.getFieldDisplayName(field)} must be at least ${requiredLength} characters.`);
      }
      if (control.errors['email']) {
        errors.push('Invalid email format.');
      }
      if (control.errors['noDigit']) {
        errors.push('Password must contain at least one digit (0-9).');
      }
      if (control.errors['noSpecialChar']) {
        errors.push('Password must contain at least one non-alphanumeric character (e.g., !, @, #).');
      }
    }

    return errors;
  }

  private getFieldDisplayName(field: string): string {
    const displayNames: { [key: string]: string } = {
      firstName: 'First name',
      lastName: 'Last name',
      email: 'Email',
      password: 'Password'
    };
    return displayNames[field] || field;
  }

  // Get all validation errors for the form
  getAllErrors(): string[] {
    const allErrors: string[] = [...this.errors]; // Include server errors
    
    if (this.registerForm.invalid) {
      Object.keys(this.registerForm.controls).forEach(field => {
        const fieldErrors = this.getFieldErrors(field);
        allErrors.push(...fieldErrors);
      });
    }

    return allErrors;
  }

  onRegister() {
    // Mark all fields as touched to show validation errors
    Object.keys(this.registerForm.controls).forEach(key => {
      this.registerForm.get(key)?.markAsTouched();
    });

    if (this.registerForm.valid) {
      const formValues = this.registerForm.value;
      this.authService
        .register(formValues.firstName, formValues.lastName, formValues.email, formValues.password)
        .subscribe({
          next: (response) => {
            this.router.navigate(["/login"]);
          },
          error: (err) => {
            console.error("Registration failed", err);
            this.errors = [];
            if (err.error && err.error.errors) {
              this.errors = err.error.errors.map((e: any) => e.description || e);
            } else if (err.error && err.error.message) {
              this.errors.push(err.error.message);
            } else {
              this.errors.push("An unexpected error occurred. Please try again.");
            }
          },
        });
    }
  }
}
