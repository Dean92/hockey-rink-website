import { Component, OnInit, signal, computed } from '@angular/core';
import { AuthService } from '../auth';
import { DataService } from '../data';
import { Router, ActivatedRoute, RouterLink } from '@angular/router';
import {
  ReactiveFormsModule,
  FormBuilder,
  FormGroup,
  Validators,
} from '@angular/forms';
import { CommonModule, DatePipe } from '@angular/common';
import { Session, SessionRegistrationRequest } from '../models';
import { provideNgxMask, NgxMaskDirective } from 'ngx-mask';
import { ToastService } from '../services/toast.service';
import { ToastContainerComponent } from '../toast-container/toast-container.component';

@Component({
  selector: 'app-session-registration',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    DatePipe,
    RouterLink,
    NgxMaskDirective,
    ToastContainerComponent,
  ],
  providers: [provideNgxMask()],
  templateUrl: './session-registration.html',
  styleUrl: './session-registration.css',
})
export class SessionRegistration implements OnInit {
  sessions = signal<Session[]>([]);
  errorMessage = signal<string | null>(null);
  successMessage = signal<string | null>(null);
  isLoading = signal<boolean>(true);
  registrationForm: FormGroup;
  selectedSession = signal<Session | null>(null);
  currentStep = signal<number>(1);
  isProcessingPayment = signal<boolean>(false);

  // Check if selected session has a league (for position field display)
  isLeagueSession = computed(() => {
    const session = this.selectedSession();
    return session?.leagueId !== null && session?.leagueId !== undefined;
  });

  constructor(
    private authService: AuthService,
    private dataService: DataService,
    private router: Router,
    private route: ActivatedRoute,
    private formBuilder: FormBuilder,
    private toastService: ToastService
  ) {
    this.registrationForm = this.formBuilder.group({
      // Step 1: Personal Information
      sessionId: ['', Validators.required],
      name: ['', Validators.required],
      email: ['', [Validators.required, Validators.email]],
      phone: ['', [Validators.required, Validators.pattern(/^[\d\s()+-]+$/)]],
      dateOfBirth: ['', Validators.required],
      address: [''],
      city: [''],
      state: [''],
      zipCode: [''],
      position: [''],
      emergencyContactName: ['', Validators.required],
      emergencyContactPhone: ['', Validators.required],
      agreeToTerms: [false, Validators.requiredTrue],
      // Step 2: Payment Information
      cardNumber: ['', [Validators.required, Validators.pattern(/^\d{16}$/)]],
      expiryDate: [
        '',
        [Validators.required, Validators.pattern(/^(0[1-9]|1[0-2])\/\d{2}$/)],
      ],
      cvv: ['', [Validators.required, Validators.pattern(/^\d{3,4}$/)]],
      cardholderName: ['', Validators.required],
    });
  }

  ngOnInit() {
    this.loadSessions();
    this.loadUserProfile();

    // Check if sessionId was passed via query params
    this.route.queryParams.subscribe((params) => {
      if (params['sessionId']) {
        this.registrationForm.patchValue({ sessionId: params['sessionId'] });
        this.onSessionChange();
      }
    });
  }

  loadUserProfile() {
    this.dataService.getProfile().subscribe({
      next: (profile) => {
        // Pre-fill user information from profile
        const patchData: any = {};

        if (profile.firstName && profile.lastName) {
          patchData.name = `${profile.firstName} ${profile.lastName}`;
        }

        if (profile.email) {
          patchData.email = profile.email;
        }

        if (profile.phone) {
          patchData.phone = profile.phone;
        }

        if (profile.address) {
          patchData.address = profile.address;
        }

        if (profile.city) {
          patchData.city = profile.city;
        }

        if (profile.state) {
          patchData.state = profile.state;
        }

        if (profile.zipCode) {
          patchData.zipCode = profile.zipCode;
        }

        if (profile.dateOfBirth) {
          // Format date for date input (YYYY-MM-DD)
          const date = new Date(profile.dateOfBirth);
          if (!isNaN(date.getTime())) {
            patchData.dateOfBirth = date.toISOString().split('T')[0];
          }
        }

        if (profile.position) {
          patchData.position = profile.position;
        }

        // Pre-fill emergency contact if available
        if (profile.emergencyContactName) {
          patchData.emergencyContactName = profile.emergencyContactName;
        }

        if (profile.emergencyContactPhone) {
          patchData.emergencyContactPhone = profile.emergencyContactPhone;
        }

        this.registrationForm.patchValue(patchData);
      },
      error: (err) => {
        console.error('Error loading user profile:', err);
      },
    });
  }

  loadSessions() {
    this.dataService.getSessions().subscribe({
      next: (data) => {
        console.log('Sessions fetched:', data);
        this.sessions.set(data.filter((s) => s.isActive)); // Only show active sessions
        this.isLoading.set(false);
      },
      error: (err) => {
        console.error('Error fetching sessions:', err);
        this.errorMessage.set(err.error?.message || 'Failed to fetch sessions');
        this.isLoading.set(false);
      },
    });
  }

  onSessionChange() {
    const sessionId = this.registrationForm.get('sessionId')?.value;
    if (sessionId) {
      const session = this.sessions().find((s) => s.id === parseInt(sessionId));
      this.selectedSession.set(session || null);
    } else {
      this.selectedSession.set(null);
    }
  }

  getSessionPrice(session: Session | null): number {
    if (!session) return 0;

    const now = new Date();
    if (session.earlyBirdPrice && session.earlyBirdEndDate) {
      const earlyBirdEnd = new Date(session.earlyBirdEndDate);
      if (now <= earlyBirdEnd) {
        return session.earlyBirdPrice;
      }
    }
    return session.regularPrice || session.fee;
  }

  nextStep() {
    if (this.currentStep() === 1) {
      // Validate Step 1 fields (personal info + terms)
      const step1Fields = [
        'sessionId',
        'name',
        'email',
        'dateOfBirth',
        'phone',
        'agreeToTerms',
      ];
      let isValid = true;

      step1Fields.forEach((field) => {
        const control = this.registrationForm.get(field);
        if (control && control.invalid) {
          control.markAsTouched();
          isValid = false;
        }
      });

      // Ensure session is selected
      if (!this.selectedSession()) {
        this.onSessionChange();
      }

      if (isValid && this.selectedSession()) {
        this.currentStep.set(2);
        window.scrollTo({ top: 0, behavior: 'smooth' });
      } else if (!this.selectedSession()) {
        this.errorMessage.set('Please select a session');
      }
    }
  }

  previousStep() {
    if (this.currentStep() > 1) {
      this.currentStep.set(this.currentStep() - 1);
      window.scrollTo({ top: 0, behavior: 'smooth' });
    }
  }

  hasError(field: string, errorType: string): boolean {
    const control = this.registrationForm.get(field);
    return !!(
      control &&
      control.hasError(errorType) &&
      (control.dirty || control.touched)
    );
  }

  isStep1Valid(): boolean {
    const step1Fields = [
      'sessionId',
      'name',
      'email',
      'dateOfBirth',
      'phone',
      'agreeToTerms',
    ];

    return step1Fields.every((field) => {
      const control = this.registrationForm.get(field);
      return control && control.valid;
    });
  }

  // Format card number with spaces (XXXX XXXX XXXX XXXX)
  formatCardNumber(event: any) {
    let value = event.target.value.replace(/\s/g, '');
    if (value.length > 16) {
      value = value.substring(0, 16);
    }
    const formatted = value.match(/.{1,4}/g)?.join(' ') || value;
    this.registrationForm.patchValue(
      { cardNumber: value },
      { emitEvent: false }
    );
    event.target.value = formatted;
  }

  // Format expiry date (MM/YY)
  formatExpiryDate(event: any) {
    let value = event.target.value.replace(/\D/g, '');
    if (value.length >= 2) {
      value = value.substring(0, 2) + '/' + value.substring(2, 4);
    }
    event.target.value = value;
    this.registrationForm.patchValue({ expiryDate: value });
  }

  // Format CVV (digits only)
  formatCvv(event: any) {
    let value = event.target.value.replace(/\D/g, '');
    if (value.length > 4) {
      value = value.substring(0, 4);
    }
    event.target.value = value;
    this.registrationForm.patchValue({ cvv: value });
  }

  onRegister() {
    this.errorMessage.set(null);
    this.successMessage.set(null);

    if (this.registrationForm.invalid) {
      this.registrationForm.markAllAsTouched();
      this.errorMessage.set('Please fill out all required fields correctly');
      return;
    }

    const session = this.selectedSession();
    if (session?.isFull) {
      this.errorMessage.set('This session is full');
      return;
    }

    this.isLoading.set(true);
    this.isProcessingPayment.set(true);
    const formValue = this.registrationForm.value;

    const registration: SessionRegistrationRequest = {
      sessionId: parseInt(formValue.sessionId),
      name: formValue.name,
      email: formValue.email,
      phone: formValue.phone || undefined,
      dateOfBirth: formValue.dateOfBirth,
      address: formValue.address || undefined,
      city: formValue.city || undefined,
      state: formValue.state || undefined,
      zipCode: formValue.zipCode || undefined,
      position: formValue.position || undefined,
      cardNumber: formValue.cardNumber,
      expiryDate: formValue.expiryDate,
      cvv: formValue.cvv,
      cardholderName: formValue.cardholderName,
    };

    this.dataService.registerSession(registration).subscribe({
      next: (response) => {
        console.log('Session registration successful:', response);
        this.isLoading.set(false);
        this.isProcessingPayment.set(false);

        // Show success toast
        this.toastService.success(
          'Transaction Successful!',
          `You're all set for ${session?.name}. Redirecting to dashboard...`
        );

        // Redirect to dashboard after 2 seconds
        setTimeout(() => {
          this.router.navigate(['/dashboard']);
        }, 2000);
      },
      error: (err) => {
        console.error('Session registration failed:', err);
        this.isProcessingPayment.set(false);

        let errorMsg =
          'Registration failed. Please check your payment information and try again.';

        // Check for ModelState errors (400 BadRequest with validation errors)
        if (err.status === 400 && err.error) {
          if (err.error.errors) {
            // Extract all error messages from ModelState
            const errors = Object.values(err.error.errors).flat().join(' ');
            errorMsg = errors || errorMsg;
          } else if (typeof err.error === 'object') {
            // Extract errors from empty string key (common in ModelState)
            const emptyKeyErrors = err.error[''] || err.error[''];
            if (Array.isArray(emptyKeyErrors) && emptyKeyErrors.length > 0) {
              errorMsg = emptyKeyErrors.join(' ');
            } else if (err.error.error) {
              errorMsg = err.error.error;
            } else if (err.error.message) {
              errorMsg = err.error.message;
            }
          }
        } else if (err.error?.error) {
          errorMsg = err.error.error;
        } else if (err.error?.message) {
          errorMsg = err.error.message;
        } else if (err.error?.details) {
          errorMsg = err.error.details;
        }

        this.errorMessage.set(errorMsg);

        // Show error toast
        this.toastService.error('Payment Failed', errorMsg);

        this.isLoading.set(false);
      },
    });
  }
}
