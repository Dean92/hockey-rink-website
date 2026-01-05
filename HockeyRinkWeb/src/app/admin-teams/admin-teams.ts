import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import {
  FormBuilder,
  FormGroup,
  ReactiveFormsModule,
  Validators,
} from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { DataService } from '../data';
import { ToastService } from '../services/toast.service';
import { ToastContainerComponent } from '../toast-container/toast-container.component';

interface Team {
  id: number;
  teamName: string;
  captainName?: string;
  teamColor?: string;
  createdAt: string;
}

@Component({
  selector: 'app-admin-teams',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, ToastContainerComponent],
  templateUrl: './admin-teams.html',
  styleUrls: ['./admin-teams.css'],
})
export class AdminTeams implements OnInit {
  teams = signal<Team[]>([]);
  sessionId = signal<number>(0);
  sessionName = signal<string>('');
  isLoading = signal<boolean>(true);
  showModal = signal<boolean>(false);
  isEditing = signal<boolean>(false);
  isSaving = signal<boolean>(false);
  teamForm: FormGroup;
  editingTeamId = signal<number | null>(null);

  teamColors = [
    { name: 'Blue', value: 'Blue' },
    { name: 'Red', value: 'Red' },
    { name: 'Green', value: 'Green' },
    { name: 'Yellow', value: 'Yellow' },
    { name: 'Purple', value: 'Purple' },
    { name: 'Orange', value: 'Orange' },
    { name: 'Brass', value: 'Brass' },
    { name: 'Copper', value: 'Copper' },
  ];

  constructor(
    private dataService: DataService,
    private route: ActivatedRoute,
    private router: Router,
    private formBuilder: FormBuilder,
    private toastService: ToastService
  ) {
    this.teamForm = this.formBuilder.group({
      teamName: ['', [Validators.required, Validators.maxLength(100)]],
      captainName: ['', Validators.maxLength(100)],
      teamColor: [''],
    });
  }

  ngOnInit() {
    this.route.params.subscribe((params) => {
      const id = +params['sessionId'];
      this.sessionId.set(id);
      this.loadSessionDetails();
      this.loadTeams();
    });
  }

  formatDateInCentralTime(dateString: string): string {
    // Ensure the date string is treated as UTC
    const utcDate = dateString.endsWith('Z') ? dateString : dateString + 'Z';
    const date = new Date(utcDate);
    return date.toLocaleString('en-US', {
      month: 'numeric',
      day: 'numeric',
      year: '2-digit',
      hour: 'numeric',
      minute: '2-digit',
      hour12: true,
    });
  }

  loadSessionDetails() {
    this.dataService.getSessionById(this.sessionId()).subscribe({
      next: (session) => {
        this.sessionName.set(session.name);
      },
      error: (err) => {
        console.error('Error loading session:', err);
        this.toastService.error('Error', 'Failed to load session details');
      },
    });
  }

  loadTeams() {
    this.isLoading.set(true);
    this.dataService.getTeamsForSession(this.sessionId()).subscribe({
      next: (data) => {
        this.teams.set(data);
        this.isLoading.set(false);
      },
      error: (err) => {
        console.error('Error loading teams:', err);
        this.toastService.error('Error', 'Failed to load teams');
        this.isLoading.set(false);
      },
    });
  }

  openCreateModal() {
    this.isEditing.set(false);
    this.editingTeamId.set(null);
    this.teamForm.reset();
    this.showModal.set(true);
  }

  openEditModal(team: Team) {
    this.isEditing.set(true);
    this.editingTeamId.set(team.id);
    this.teamForm.patchValue({
      teamName: team.teamName,
      captainName: team.captainName || '',
      teamColor: team.teamColor || '',
    });
    this.showModal.set(true);
  }

  closeModal() {
    this.showModal.set(false);
    this.teamForm.reset();
    this.editingTeamId.set(null);
  }

  onSubmit() {
    if (this.teamForm.invalid) {
      this.teamForm.markAllAsTouched();
      return;
    }

    this.isSaving.set(true);
    const formValue = this.teamForm.value;

    if (this.isEditing()) {
      const teamId = this.editingTeamId();
      if (teamId) {
        this.dataService.updateTeam(teamId, formValue).subscribe({
          next: () => {
            this.toastService.success('Success', 'Team updated successfully');
            this.loadTeams();
            this.closeModal();
            this.isSaving.set(false);
          },
          error: (err) => {
            console.error('Error updating team:', err);
            this.toastService.error(
              'Error',
              err.error?.message || 'Failed to update team'
            );
            this.isSaving.set(false);
          },
        });
      }
    } else {
      this.dataService.createTeam(this.sessionId(), formValue).subscribe({
        next: () => {
          this.toastService.success('Success', 'Team created successfully');
          this.loadTeams();
          this.closeModal();
          this.isSaving.set(false);
        },
        error: (err) => {
          console.error('Error creating team:', err);
          this.toastService.error(
            'Error',
            err.error?.message || 'Failed to create team'
          );
          this.isSaving.set(false);
        },
      });
    }
  }

  deleteTeam(team: Team) {
    if (!confirm(`Are you sure you want to delete team "${team.teamName}"?`)) {
      return;
    }

    this.dataService.deleteTeam(team.id).subscribe({
      next: () => {
        this.toastService.success('Success', 'Team deleted successfully');
        this.loadTeams();
      },
      error: (err) => {
        console.error('Error deleting team:', err);
        this.toastService.error(
          'Error',
          err.error?.message || 'Failed to delete team'
        );
      },
    });
  }

  goBack() {
    this.router.navigate(['/admin/sessions']);
  }

  hasError(field: string, errorType: string): boolean {
    const control = this.teamForm.get(field);
    return !!(
      control &&
      control.hasError(errorType) &&
      (control.dirty || control.touched)
    );
  }
}
