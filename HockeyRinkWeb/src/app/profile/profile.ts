import { Component, OnInit, signal } from '@angular/core';
import { DataService } from '../data';
import { CommonModule } from '@angular/common';
import {
  ReactiveFormsModule,
  FormBuilder,
  FormGroup,
  Validators,
} from '@angular/forms';
import { UserProfile } from '../models';
import { ToastService } from '../services/toast.service';
import { ToastContainerComponent } from '../toast-container/toast-container.component';

@Component({
  selector: 'app-profile',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, ToastContainerComponent],
  templateUrl: './profile.html',
  styleUrls: ['./profile.css'],
})
export class Profile implements OnInit {
  profile = signal<UserProfile | null>(null);
  leagues = signal<any[]>([]);
  errorMessage = signal<string | null>(null);
  successMessage = signal<string | null>(null);
  isLoading = signal<boolean>(true);
  isEditing = signal<boolean>(false);
  isSaving = signal<boolean>(false);
  showChangePassword = signal<boolean>(false);
  isChangingPassword = signal<boolean>(false);
  profileForm: FormGroup;
  changePasswordForm: FormGroup;

  constructor(
    private dataService: DataService,
    private formBuilder: FormBuilder,
    private toastService: ToastService
  ) {
    this.profileForm = this.formBuilder.group({
      address: ['', Validators.required],
      city: ['', Validators.required],
      state: ['', Validators.required],
      zipCode: ['', Validators.required],
      phone: ['', [Validators.required, Validators.pattern(/^[\d\s()+-]+$/)]],
      dateOfBirth: ['', Validators.required],
      position: ['', Validators.required],
    });

    this.changePasswordForm = this.formBuilder.group(
      {
        currentPassword: ['', Validators.required],
        newPassword: ['', [Validators.required, Validators.minLength(6)]],
        confirmPassword: ['', Validators.required],
      },
      { validators: this.passwordMatchValidator }
    );
  }

  ngOnInit() {
    this.loadProfile();
    this.loadLeagues();
  }

  loadLeagues() {
    this.dataService.getLeagues().subscribe({
      next: (data) => {
        this.leagues.set(data);
      },
      error: (err) => {
        console.error('Error fetching leagues:', err);
      },
    });
  }

  loadProfile() {
    this.isLoading.set(true);
    this.dataService.getProfile().subscribe({
      next: (data) => {
        this.profile.set(data);
        this.populateForm(data);
        this.isLoading.set(false);
      },
      error: (err) => {
        console.error('Error fetching profile:', err);
        this.errorMessage.set(
          err.error?.message || 'Failed to fetch profile. Please try again.'
        );
        this.isLoading.set(false);
      },
    });
  }

  populateForm(profile: UserProfile) {
    this.profileForm.patchValue({
      address: profile.address || '',
      city: profile.city || '',
      state: profile.state || '',
      zipCode: profile.zipCode || '',
      phone: profile.phone || '',
      dateOfBirth: profile.dateOfBirth
        ? new Date(profile.dateOfBirth).toISOString().split('T')[0]
        : '',
      position: profile.position || '',
    });
  }

  getLeagueName(leagueId: number | undefined): string {
    const profile = this.profile();
    if (profile?.leagueName) {
      return profile.leagueName;
    }
    if (!leagueId) return 'Not assigned';
    const league = this.leagues().find((l) => l.id === leagueId);
    return league ? league.name : 'Not assigned';
  }

  toggleEdit() {
    this.isEditing.set(!this.isEditing());
    if (!this.isEditing()) {
      // Reset form when canceling edit
      const currentProfile = this.profile();
      if (currentProfile) {
        this.populateForm(currentProfile);
      }
    }
  }

  onSaveProfile() {
    if (this.profileForm.invalid) {
      this.profileForm.markAllAsTouched();
      return;
    }

    this.isSaving.set(true);
    this.errorMessage.set(null);

    this.dataService.updateProfile(this.profileForm.value).subscribe({
      next: (response) => {
        this.profile.set(response);
        this.populateForm(response);
        this.isEditing.set(false);
        this.isSaving.set(false);
        this.toastService.success(
          'Profile Updated',
          'Your profile has been successfully updated'
        );
        // Reload profile to ensure we have all updated data
        this.loadProfile();
      },
      error: (err) => {
        console.error('Error updating profile:', err);
        this.errorMessage.set(
          err.error?.message || 'Failed to update profile. Please try again.'
        );
        this.toastService.error(
          'Update Failed',
          err.error?.message || 'Failed to update profile'
        );
        this.isSaving.set(false);
      },
    });
  }

  hasError(field: string, errorType: string): boolean {
    const control = this.profileForm.get(field);
    return !!(
      control &&
      control.hasError(errorType) &&
      (control.dirty || control.touched)
    );
  }

  passwordMatchValidator(formGroup: FormGroup) {
    const newPassword = formGroup.get('newPassword')?.value;
    const confirmPassword = formGroup.get('confirmPassword')?.value;
    return newPassword === confirmPassword ? null : { passwordMismatch: true };
  }

  toggleChangePassword() {
    this.showChangePassword.set(!this.showChangePassword());
    if (!this.showChangePassword()) {
      this.changePasswordForm.reset();
    }
  }

  onChangePassword() {
    if (this.changePasswordForm.invalid) {
      this.changePasswordForm.markAllAsTouched();
      return;
    }

    this.isChangingPassword.set(true);
    this.errorMessage.set(null);

    this.dataService.changePassword(this.changePasswordForm.value).subscribe({
      next: (response) => {
        this.toastService.success(
          'Password Changed',
          'Your password has been successfully updated'
        );
        this.changePasswordForm.reset();
        this.showChangePassword.set(false);
        this.isChangingPassword.set(false);
      },
      error: (err) => {
        console.error('Error changing password:', err);
        this.errorMessage.set(
          err.error?.message || 'Failed to change password. Please try again.'
        );
        this.toastService.error(
          'Password Change Failed',
          err.error?.message || 'Failed to change password'
        );
        this.isChangingPassword.set(false);
      },
    });
  }

  hasPasswordError(field: string, errorType: string): boolean {
    const control = this.changePasswordForm.get(field);
    return !!(
      control &&
      control.hasError(errorType) &&
      (control.dirty || control.touched)
    );
  }

  get passwordMismatch(): boolean {
    return (
      this.changePasswordForm.hasError('passwordMismatch') &&
      this.changePasswordForm.get('confirmPassword')?.touched === true
    );
  }

  onPhoneInput(event: Event): void {
    const input = event.target as HTMLInputElement;
    const formatted = this.formatPhone(input.value);
    this.profileForm.patchValue({ phone: formatted }, { emitEvent: false });
    input.value = formatted;
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
    // Format partial input
    if (cleaned.length > 6) {
      return `(${cleaned.substring(0, 3)}) ${cleaned.substring(
        3,
        6
      )}-${cleaned.substring(6, 10)}`;
    }
    if (cleaned.length > 3) {
      return `(${cleaned.substring(0, 3)}) ${cleaned.substring(3, 6)}`;
    }
    if (cleaned.length > 0) {
      return `(${cleaned}`;
    }
    return phone; // Return original if no digits
  }
}
