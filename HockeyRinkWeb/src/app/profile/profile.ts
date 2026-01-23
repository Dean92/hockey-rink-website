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
import { ActivatedRoute, Router } from '@angular/router';
import { AdminService } from '../admin.service';
import { AuthService } from '../auth';

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
  userId = signal<string | null>(null);
  isAdmin = signal<boolean>(false);
  profileForm: FormGroup;
  changePasswordForm: FormGroup;

  constructor(
    private dataService: DataService,
    private formBuilder: FormBuilder,
    private toastService: ToastService,
    private route: ActivatedRoute,
    private router: Router,
    private adminService: AdminService,
    private authService: AuthService
  ) {
    this.profileForm = this.formBuilder.group({
      firstName: ['', Validators.required],
      lastName: ['', Validators.required],
      email: ['', [Validators.required, Validators.email]],
      address: ['', Validators.required],
      city: ['', Validators.required],
      state: ['', Validators.required],
      zipCode: ['', Validators.required],
      phone: ['', [Validators.required, Validators.pattern(/^[\d\s()+-]+$/)]],
      dateOfBirth: ['', Validators.required],
      position: ['', Validators.required],
      rating: [null, [Validators.min(1), Validators.max(5)]],
      playerNotes: [''],
      leagueId: [null],
      emergencyContactName: [''], // Optional - filled during session registration
      emergencyContactPhone: [''], // Optional - filled during session registration
      hockeyRegistrationNumber: [''],
      hockeyRegistrationType: [''],
    });

    this.changePasswordForm = this.formBuilder.group(
      {
        currentPassword: ['', Validators.required],
        newPassword: ['', [Validators.required, Validators.minLength(6)]],
        confirmPassword: ['', Validators.required],
      },
      { validators: this.passwordMatchValidator.bind(this) }
    );
  }

  ngOnInit() {
    // Check if viewing another user's profile (admin only)
    this.route.queryParams.subscribe((params) => {
      const userId = params['userId'];
      if (userId) {
        this.userId.set(userId);
        console.log('UserId set to:', userId);
      }
    });

    // Check if current user is admin
    const adminStatus = this.authService.isAdmin();
    this.isAdmin.set(adminStatus);
    console.log('Is Admin:', adminStatus, 'UserId:', this.userId());

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
    const userId = this.userId();

    // If userId is set and user is admin, load that user's profile
    if (userId && this.isAdmin()) {
      this.loadUserProfile(userId);
    } else {
      // Otherwise load current user's profile
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
  }

  loadUserProfile(userId: string) {
    // Load user via admin endpoint to get rating and notes
    this.adminService.getUsers().subscribe({
      next: (users) => {
        const user = users.find((u) => u.id === userId);
        if (user) {
          console.log('Found user:', user);
          const profileData: any = {
            email: user.email,
            firstName: user.firstName,
            lastName: user.lastName,
            position: user.position,
            rating: user.rating,
            playerNotes: user.playerNotes,
            address: user.address,
            city: user.city,
            state: user.state,
            zipCode: user.zipCode,
            phone: user.phone,
            dateOfBirth: user.dateOfBirth,
            leagueId: user.leagueId,
            leagueName: user.leagueName,
            emergencyContactName: user.emergencyContactName,
            emergencyContactPhone: user.emergencyContactPhone,
            hockeyRegistrationNumber: user.hockeyRegistrationNumber,
            hockeyRegistrationType: user.hockeyRegistrationType,
          };
          console.log('Profile data set:', profileData);
          this.profile.set(profileData);
          this.populateForm(profileData);
        }
        this.isLoading.set(false);
      },
      error: (err) => {
        console.error('Error fetching user profile:', err);
        this.errorMessage.set('Failed to fetch user profile');
        this.isLoading.set(false);
      },
    });
  }

  populateForm(profile: any) {
    this.profileForm.patchValue({
      firstName: profile.firstName || '',
      lastName: profile.lastName || '',
      email: profile.email || '',
      address: profile.address || '',
      city: profile.city || '',
      state: profile.state || '',
      zipCode: profile.zipCode || '',
      phone: profile.phone || '',
      dateOfBirth: profile.dateOfBirth
        ? new Date(profile.dateOfBirth).toISOString().split('T')[0]
        : '',
      position: profile.position || '',
      rating: profile.rating || null,
      playerNotes: profile.playerNotes || '',
      leagueId: profile.leagueId || null,
      emergencyContactName: profile.emergencyContactName || '',
      emergencyContactPhone: profile.emergencyContactPhone || '',
      hockeyRegistrationNumber: profile.hockeyRegistrationNumber || '',
      hockeyRegistrationType: profile.hockeyRegistrationType || '',
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

    // Format phone before saving
    const formValue = { ...this.profileForm.value };
    formValue.phone = this.formatPhone(formValue.phone);

    // If viewing another user's profile as admin, update via admin endpoint
    if (this.userId() && this.isAdmin()) {
      this.adminService.updateUserProfile(this.userId()!, formValue).subscribe({
        next: () => {
          this.isSaving.set(false);
          this.isEditing.set(false);
          this.toastService.success(
            'Success',
            'Player profile updated successfully'
          );
          this.loadProfile();
        },
        error: (err) => {
          console.error('Error updating profile:', err);
          this.isSaving.set(false);
          const errorMessage =
            err.error?.message || 'Failed to update player information';
          this.toastService.error('Update Failed', errorMessage);
        },
      });
    } else {
      // Regular profile update for current user
      this.dataService.updateProfile(formValue).subscribe({
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

  backToUsers() {
    this.router.navigate(['/admin/users']);
  }
}
