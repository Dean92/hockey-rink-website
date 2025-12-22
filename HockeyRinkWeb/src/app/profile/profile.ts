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
  errorMessage = signal<string | null>(null);
  successMessage = signal<string | null>(null);
  isLoading = signal<boolean>(true);
  isEditing = signal<boolean>(false);
  isSaving = signal<boolean>(false);
  profileForm: FormGroup;

  constructor(
    private dataService: DataService,
    private formBuilder: FormBuilder,
    private toastService: ToastService
  ) {
    this.profileForm = this.formBuilder.group({
      address: [''],
      city: [''],
      state: [''],
      zipCode: [''],
      phone: ['', Validators.pattern(/^[\d\s()+-]+$/)],
      dateOfBirth: [''],
      position: [''],
    });
  }

  ngOnInit() {
    this.loadProfile();
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
}
