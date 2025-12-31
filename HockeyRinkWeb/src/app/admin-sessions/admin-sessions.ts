import { Component, OnInit, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import {
  ReactiveFormsModule,
  FormBuilder,
  FormGroup,
  Validators,
} from '@angular/forms';
import { RouterLink } from '@angular/router';
import { NgxMaskDirective, provideNgxMask } from 'ngx-mask';
import { DataService } from '../data';
import { Session, League } from '../models';

@Component({
  selector: 'app-admin-sessions',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink, NgxMaskDirective],
  providers: [provideNgxMask()],
  templateUrl: './admin-sessions.html',
  styleUrls: ['./admin-sessions.css'],
})
export class AdminSessions implements OnInit {
  sessions = signal<Session[]>([]);
  activeFilter = signal<'all' | 'active' | 'inactive'>('all');
  filteredSessions = computed(() => {
    const filter = this.activeFilter();
    const allSessions = this.sessions();
    if (filter === 'all') return allSessions;
    if (filter === 'active') return allSessions.filter((s) => s.isActive);
    return allSessions.filter((s) => !s.isActive);
  });
  leagues = signal<League[]>([]);
  errorMessage = signal<string | null>(null);
  successMessage = signal<string | null>(null);
  isLoading = signal<boolean>(false);
  showModal = signal<boolean>(false);
  showDeleteModal = signal<boolean>(false);
  showRegistrationsModal = signal<boolean>(false);
  showAddRegistrationModal = signal<boolean>(false);
  showRemoveConfirmModal = signal<boolean>(false);
  isEditMode = signal<boolean>(false);
  isEditingRegistration = signal<boolean>(false);
  currentRegistrationId: number | null = null;
  sessionForm: FormGroup;
  manualRegistrationForm: FormGroup;
  currentSessionId: number | null = null;
  sessionToDelete: { id: number; name: string } | null = null;
  currentSession = signal<any>(null);
  registrations = signal<any[]>([]);
  registrationToRemove = signal<any>(null);
  passwordSetupLink = signal<string | null>(null);

  constructor(private dataService: DataService, private fb: FormBuilder) {
    this.sessionForm = this.fb.group({
      name: ['', [Validators.required, Validators.maxLength(100)]],
      startDate: ['', Validators.required],
      endDate: ['', Validators.required],
      fee: [0],
      isActive: [false],
      leagueId: ['', Validators.required],
      maxPlayers: [
        20,
        [Validators.required, Validators.min(1), Validators.max(250)],
      ],
      registrationOpenDate: [''],
      registrationCloseDate: [''],
      earlyBirdPrice: [null, [Validators.min(0), Validators.max(1000)]],
      earlyBirdEndDate: [''],
      regularPrice: [
        0,
        [Validators.required, Validators.min(0), Validators.max(1000)],
      ],
    });

    // Auto-populate fee from regular price
    this.sessionForm.get('regularPrice')?.valueChanges.subscribe((value) => {
      this.sessionForm.patchValue({ fee: value || 0 }, { emitEvent: false });
    });

    // Initialize manual registration form
    this.manualRegistrationForm = this.fb.group({
      name: ['', [Validators.required, Validators.maxLength(100)]],
      email: ['', [Validators.required, Validators.email]],
      phone: ['', Validators.required],
      address: ['', [Validators.required, Validators.maxLength(200)]],
      city: ['', [Validators.required, Validators.maxLength(100)]],
      state: ['', [Validators.required, Validators.maxLength(2)]],
      zipCode: ['', [Validators.required, Validators.maxLength(10)]],
      dateOfBirth: ['', Validators.required],
      position: ['', Validators.required],
      amountPaid: [0, [Validators.required, Validators.min(0)]],
    });
  }

  ngOnInit(): void {
    this.loadLeagues();
    this.loadSessions();
  }

  loadLeagues(): void {
    this.dataService.getLeagues().subscribe({
      next: (data) => this.leagues.set(data),
      error: (err) => {
        console.error('Error loading leagues:', err);
        this.errorMessage.set('Failed to load leagues');
      },
    });
  }

  loadSessions(): void {
    this.isLoading.set(true);
    this.dataService.getAllSessions().subscribe({
      next: (data) => {
        this.sessions.set(data);
        this.isLoading.set(false);
      },
      error: (err) => {
        console.error('Error loading sessions:', err);
        this.errorMessage.set('Failed to load sessions');
        this.isLoading.set(false);
      },
    });
  }

  openCreateModal(): void {
    this.isEditMode.set(false);
    this.currentSessionId = null;
    this.sessionForm.reset({
      name: '',
      startDate: '',
      endDate: '',
      fee: 0,
      isActive: false,
      leagueId: '',
      maxPlayers: 20,
      registrationOpenDate: '',
      registrationCloseDate: '',
      earlyBirdPrice: null,
      earlyBirdEndDate: '',
      regularPrice: 0,
    });
    this.showModal.set(true);
    this.errorMessage.set(null);
    this.successMessage.set(null);
  }

  openEditModal(session: Session): void {
    this.isEditMode.set(true);
    this.currentSessionId = session.id;
    this.sessionForm.patchValue({
      name: session.name,
      startDate: this.formatDateForInput(session.startDate),
      endDate: this.formatDateForInput(session.endDate),
      fee: session.fee,
      isActive: session.isActive,
      leagueId: session.leagueId,
      maxPlayers: session.maxPlayers || 20,
      registrationOpenDate: session.registrationOpenDate
        ? this.formatDateTimeForInput(session.registrationOpenDate)
        : '',
      registrationCloseDate: session.registrationCloseDate
        ? this.formatDateTimeForInput(session.registrationCloseDate)
        : '',
      earlyBirdPrice: session.earlyBirdPrice,
      earlyBirdEndDate: session.earlyBirdEndDate
        ? this.formatDateTimeForInput(session.earlyBirdEndDate)
        : '',
      regularPrice: session.regularPrice,
    });
    this.showModal.set(true);
    this.errorMessage.set(null);
    this.successMessage.set(null);
  }

  closeModal(): void {
    this.showModal.set(false);
    this.sessionForm.reset();
    this.currentSessionId = null;
  }

  onSubmit(): void {
    if (this.sessionForm.invalid) {
      this.sessionForm.markAllAsTouched();
      return;
    }

    this.isLoading.set(true);
    this.errorMessage.set(null);
    this.successMessage.set(null);

    const formData = this.sessionForm.value;

    // Helper function to convert local datetime-local input to UTC ISO string
    const toUTC = (dateTimeLocal: string | null): string | null => {
      if (!dateTimeLocal) return null;
      // datetime-local gives us a string like "2025-12-16T16:20"
      // We need to treat this as local time and convert to UTC
      const localDate = new Date(dateTimeLocal);
      return localDate.toISOString();
    };

    // Convert empty strings to null for datetime fields and convert to UTC
    const cleanedData = {
      ...formData,
      registrationOpenDate: toUTC(formData.registrationOpenDate),
      registrationCloseDate: toUTC(formData.registrationCloseDate),
      earlyBirdEndDate: toUTC(formData.earlyBirdEndDate),
    };

    // Log the datetime values being sent
    console.log('Form data being submitted:', {
      registrationOpenDate: cleanedData.registrationOpenDate,
      registrationCloseDate: cleanedData.registrationCloseDate,
      earlyBirdEndDate: cleanedData.earlyBirdEndDate,
    });

    if (this.isEditMode() && this.currentSessionId) {
      // Update existing session
      this.dataService
        .updateSession(this.currentSessionId, cleanedData)
        .subscribe({
          next: (response) => {
            this.successMessage.set(
              response.message || 'Session updated successfully'
            );
            this.isLoading.set(false);
            this.closeModal();
            this.loadSessions();
          },
          error: (err) => {
            console.error('Error updating session:', err);
            const sessionName = this.sessionForm.get('name')?.value;
            this.errorMessage.set(
              err.error?.message || `Failed to update session "${sessionName}"`
            );
            this.isLoading.set(false);
          },
        });
    } else {
      // Create new session
      this.dataService.createSession(cleanedData).subscribe({
        next: (response) => {
          this.successMessage.set(
            response.message || 'Session created successfully'
          );
          this.isLoading.set(false);
          this.closeModal();
          this.loadSessions();
        },
        error: (err) => {
          console.error('Error creating session:', err);
          this.errorMessage.set(
            err.error?.message || 'Failed to create session'
          );
          this.isLoading.set(false);
        },
      });
    }
  }

  openDeleteModal(id: number, name: string): void {
    this.sessionToDelete = { id, name };
    this.showDeleteModal.set(true);
    this.errorMessage.set(null);
    this.successMessage.set(null);
  }

  closeDeleteModal(): void {
    this.showDeleteModal.set(false);
    this.sessionToDelete = null;
  }

  confirmDelete(): void {
    if (!this.sessionToDelete) {
      return;
    }

    const { id, name } = this.sessionToDelete;
    this.isLoading.set(true);
    this.dataService.deleteSession(id).subscribe({
      next: (response) => {
        this.successMessage.set(`Session "${name}" deleted successfully`);
        this.isLoading.set(false);
        this.closeDeleteModal();
        this.loadSessions();
      },
      error: (err) => {
        console.error('Error deleting session:', err);
        this.errorMessage.set(err.error?.message || 'Failed to delete session');
        this.isLoading.set(false);
        this.closeDeleteModal();
      },
    });
  }

  deleteSession(id: number, name: string): void {
    this.openDeleteModal(id, name);
  }

  formatDateForInput(dateString: string): string {
    const date = new Date(dateString);
    // Return YYYY-MM-DD format for date inputs
    const year = date.getFullYear();
    const month = String(date.getMonth() + 1).padStart(2, '0');
    const day = String(date.getDate()).padStart(2, '0');
    return `${year}-${month}-${day}`;
  }

  formatDateTimeForInput(dateString: string): string {
    // Ensure the date string is treated as UTC by appending 'Z' if it doesn't have timezone info
    let utcString = dateString;
    if (
      !dateString.endsWith('Z') &&
      !dateString.includes('+') &&
      !dateString.includes('T')
    ) {
      // If it's just a date without time, don't modify
      return dateString;
    }
    if (!dateString.endsWith('Z') && !dateString.includes('+')) {
      // Add 'Z' to indicate UTC if not present
      utcString = dateString + 'Z';
    }

    const date = new Date(utcString);

    // Return YYYY-MM-DDTHH:mm format for datetime-local inputs in local time
    const year = date.getFullYear();
    const month = String(date.getMonth() + 1).padStart(2, '0');
    const day = String(date.getDate()).padStart(2, '0');
    const hours = String(date.getHours()).padStart(2, '0');
    const minutes = String(date.getMinutes()).padStart(2, '0');
    return `${year}-${month}-${day}T${hours}:${minutes}`;
  }

  formatPhone(phone: string): string {
    if (!phone) return '';
    // Remove all non-numeric characters
    const cleaned = phone.replace(/\D/g, '');
    // Format as (XXX) XXX-XXXX
    if (cleaned.length === 10) {
      return `(${cleaned.substring(0, 3)}) ${cleaned.substring(
        3,
        6
      )}-${cleaned.substring(6)}`;
    }
    return phone; // Return original if not 10 digits
  }

  formatDate(dateString: string): string {
    return new Date(dateString).toLocaleDateString();
  }

  setFilter(filter: 'all' | 'active' | 'inactive'): void {
    this.activeFilter.set(filter);
  }

  // View Registrations
  viewRegistrations(session: Session): void {
    this.isLoading.set(true);
    this.dataService.getSessionRegistrations(session.id).subscribe({
      next: (data: any) => {
        // Backend returns an object with a registrations array
        // Store the session data with maxPlayers from response
        this.currentSession.set({
          ...session,
          maxPlayers: data.maxPlayers,
        });
        this.registrations.set(data.registrations || []);
        this.showRegistrationsModal.set(true);
        this.isLoading.set(false);
      },
      error: (err) => {
        console.error('Error loading registrations:', err);
        this.errorMessage.set('Failed to load registrations');
        this.isLoading.set(false);
      },
    });
  }

  closeRegistrationsModal(): void {
    this.showRegistrationsModal.set(false);
    this.currentSession.set(null);
    this.registrations.set([]);
  }

  // Add User to Session
  openAddRegistrationModal(session?: Session): void {
    if (session) {
      this.currentSession.set(session);
    }
    const current = this.currentSession();
    if (!current) return;

    this.manualRegistrationForm.reset({
      amountPaid: current.regularPrice || current.fee || 0,
    });
    this.showAddRegistrationModal.set(true);
  }

  closeAddRegistrationModal(): void {
    this.showAddRegistrationModal.set(false);
    this.isEditingRegistration.set(false);
    this.currentRegistrationId = null;
    this.manualRegistrationForm.reset();
  }

  submitManualRegistration(): void {
    const current = this.currentSession();
    if (this.manualRegistrationForm.invalid || !current) {
      return;
    }

    this.isLoading.set(true);

    if (this.isEditingRegistration() && this.currentRegistrationId) {
      // Update existing registration
      this.dataService
        .updateRegistration(
          current.id,
          this.currentRegistrationId,
          this.manualRegistrationForm.value
        )
        .subscribe({
          next: () => {
            this.successMessage.set('Registration updated successfully');
            this.closeAddRegistrationModal();
            // Reload registrations if modal is open, then reload sessions
            if (this.showRegistrationsModal()) {
              const current = this.currentSession();
              if (current) {
                this.viewRegistrations(current);
              }
            }
            this.loadSessions();
            this.isLoading.set(false);
          },
          error: (err) => {
            console.error('Error updating registration:', err);
            this.errorMessage.set(
              err.error?.message || 'Failed to update registration'
            );
            this.isLoading.set(false);
          },
        });
    } else {
      // Add new registration
      this.dataService
        .addManualRegistration(current.id, this.manualRegistrationForm.value)
        .subscribe({
          next: (response: any) => {
            // Show password setup link if applicable
            if (response.passwordSetupRequired && response.passwordSetupToken) {
              const setupLink = `${window.location.origin}/setup-password/${response.passwordSetupToken}`;
              this.passwordSetupLink.set(setupLink);
              this.successMessage.set(
                `User successfully added! New user needs to set up their password.`
              );
              // Copy to clipboard
              navigator.clipboard
                .writeText(setupLink)
                .then(() => {
                  console.log('Password setup link copied to clipboard');
                })
                .catch(() => {
                  console.warn('Failed to copy to clipboard');
                });
            } else {
              this.successMessage.set('User successfully added to session');
              this.passwordSetupLink.set(null);
            }

            this.closeAddRegistrationModal();
            // Reload registrations if modal is open, then reload sessions
            if (this.showRegistrationsModal()) {
              const current = this.currentSession();
              if (current) {
                this.viewRegistrations(current);
              }
            }
            this.loadSessions();
            this.isLoading.set(false);
          },
          error: (err) => {
            console.error('Error adding manual registration:', err);
            this.errorMessage.set(
              err.error?.message || 'Failed to add user to session'
            );
            this.isLoading.set(false);
          },
        });
    }
  }

  // Edit Registration
  editRegistration(registration: any): void {
    this.isEditingRegistration.set(true);
    this.currentRegistrationId = registration.id;

    // Populate form with registration data
    this.manualRegistrationForm.patchValue({
      name: registration.name,
      email: registration.email,
      phone: registration.phone,
      address: registration.address,
      city: registration.city,
      state: registration.state,
      zipCode: registration.zipCode,
      dateOfBirth: registration.dateOfBirth?.split('T')[0], // Format for date input
      position: registration.position,
      amountPaid: registration.amountPaid,
    });

    this.showAddRegistrationModal.set(true);
  }

  // Remove User from Session
  confirmRemoveRegistration(registration: any): void {
    this.registrationToRemove.set(registration);
    this.showRemoveConfirmModal.set(true);
  }

  closeRemoveConfirmModal(): void {
    this.showRemoveConfirmModal.set(false);
    this.registrationToRemove.set(null);
  }

  removeRegistration(): void {
    const regToRemove = this.registrationToRemove();
    const current = this.currentSession();
    if (!regToRemove || !current) {
      return;
    }

    this.isLoading.set(true);
    this.dataService.removeRegistration(current.id, regToRemove.id).subscribe({
      next: () => {
        this.successMessage.set(
          `${regToRemove.name} successfully removed from ${current.name}`
        );
        this.closeRemoveConfirmModal();
        // Reload registrations first to update the modal, then reload sessions
        const updatedSession = this.currentSession();
        if (updatedSession) {
          this.viewRegistrations(updatedSession);
        }
        this.loadSessions();
        this.isLoading.set(false);
      },
      error: (err) => {
        console.error('Error removing registration:', err);
        this.errorMessage.set(
          err.error?.message || 'Failed to remove user from session'
        );
        this.isLoading.set(false);
      },
    });
  }

  copyPasswordSetupLink(): void {
    const link = this.passwordSetupLink();
    if (link) {
      navigator.clipboard
        .writeText(link)
        .then(() => {
          // Show a brief success indication
          const originalMessage = this.successMessage();
          this.successMessage.set('Password setup link copied to clipboard!');
          setTimeout(() => {
            this.successMessage.set(originalMessage);
          }, 2000);
        })
        .catch(() => {
          console.error('Failed to copy to clipboard');
        });
    }
  }
}
