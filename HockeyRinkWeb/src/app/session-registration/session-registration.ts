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

@Component({
  selector: 'app-session-registration',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, DatePipe, RouterLink],
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

  constructor(
    private authService: AuthService,
    private dataService: DataService,
    private router: Router,
    private route: ActivatedRoute,
    private formBuilder: FormBuilder
  ) {
    this.registrationForm = this.formBuilder.group({
      sessionId: ['', Validators.required],
      name: ['', Validators.required],
      email: ['', [Validators.required, Validators.email]],
      phone: ['', Validators.pattern(/^[\d\s()+-]+$/)],
      dateOfBirth: ['', Validators.required],
      address: [''],
      city: [''],
      state: [''],
      zipCode: [''],
      position: [''],
      agreeToTerms: [false, Validators.requiredTrue],
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
        // Pre-fill user information
        if (profile.firstName && profile.lastName) {
          this.registrationForm.patchValue({
            name: `${profile.firstName} ${profile.lastName}`,
            email: profile.email,
          });
        }
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

  getSessionPrice(session: Session): number {
    const now = new Date();
    if (session.earlyBirdPrice && session.earlyBirdEndDate) {
      const earlyBirdEnd = new Date(session.earlyBirdEndDate);
      if (now <= earlyBirdEnd) {
        return session.earlyBirdPrice;
      }
    }
    return session.regularPrice || session.fee;
  }

  hasError(field: string, errorType: string): boolean {
    const control = this.registrationForm.get(field);
    return !!(
      control &&
      control.hasError(errorType) &&
      (control.dirty || control.touched)
    );
  }

  onRegister() {
    this.errorMessage.set(null);
    this.successMessage.set(null);

    if (this.registrationForm.invalid) {
      this.registrationForm.markAllAsTouched();
      return;
    }

    const session = this.selectedSession();
    if (session?.isFull) {
      this.errorMessage.set('This session is full');
      return;
    }

    this.isLoading.set(true);
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
    };

    this.dataService.registerSession(registration).subscribe({
      next: (response) => {
        console.log('Session registration successful:', response);
        this.successMessage.set(
          `Successfully registered for ${session?.name}! Redirecting...`
        );
        this.isLoading.set(false);

        // Redirect to dashboard after 2 seconds
        setTimeout(() => {
          this.router.navigate(['/dashboard']);
        }, 2000);
      },
      error: (err) => {
        console.error('Session registration failed:', err);
        this.errorMessage.set(
          err.error?.message ||
            'Failed to register for session. Please try again.'
        );
        this.isLoading.set(false);
      },
    });
  }
}
