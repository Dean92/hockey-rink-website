import { Component, OnInit, signal } from "@angular/core";
import { CommonModule } from "@angular/common";
import {
  ReactiveFormsModule,
  FormBuilder,
  FormGroup,
  Validators,
} from "@angular/forms";
import { DataService } from "../data";
import { Session, League } from "../models";

@Component({
  selector: "app-admin-sessions",
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: "./admin-sessions.html",
  styleUrls: ["./admin-sessions.css"],
})
export class AdminSessions implements OnInit {
  sessions = signal<Session[]>([]);
  leagues = signal<League[]>([]);
  errorMessage = signal<string | null>(null);
  successMessage = signal<string | null>(null);
  isLoading = signal<boolean>(false);
  showModal = signal<boolean>(false);
  isEditMode = signal<boolean>(false);
  sessionForm: FormGroup;
  currentSessionId: number | null = null;

  constructor(private dataService: DataService, private fb: FormBuilder) {
    this.sessionForm = this.fb.group({
      name: ["", [Validators.required, Validators.maxLength(100)]],
      startDate: ["", Validators.required],
      endDate: ["", Validators.required],
      fee: [0, [Validators.required, Validators.min(0), Validators.max(1000)]],
      isActive: [true],
      leagueId: ["", Validators.required],
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
        console.error("Error loading leagues:", err);
        this.errorMessage.set("Failed to load leagues");
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
        console.error("Error loading sessions:", err);
        this.errorMessage.set("Failed to load sessions");
        this.isLoading.set(false);
      },
    });
  }

  openCreateModal(): void {
    this.isEditMode.set(false);
    this.currentSessionId = null;
    this.sessionForm.reset({
      name: "",
      startDate: "",
      endDate: "",
      fee: 0,
      isActive: true,
      leagueId: "",
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

    if (this.isEditMode() && this.currentSessionId) {
      // Update existing session
      this.dataService
        .updateSession(this.currentSessionId, formData)
        .subscribe({
          next: () => {
            this.successMessage.set("Session updated successfully");
            this.isLoading.set(false);
            this.closeModal();
            this.loadSessions();
          },
          error: (err) => {
            console.error("Error updating session:", err);
            this.errorMessage.set(
              err.error?.message || "Failed to update session"
            );
            this.isLoading.set(false);
          },
        });
    } else {
      // Create new session
      this.dataService.createSession(formData).subscribe({
        next: () => {
          this.successMessage.set("Session created successfully");
          this.isLoading.set(false);
          this.closeModal();
          this.loadSessions();
        },
        error: (err) => {
          console.error("Error creating session:", err);
          this.errorMessage.set(
            err.error?.message || "Failed to create session"
          );
          this.isLoading.set(false);
        },
      });
    }
  }

  deleteSession(id: number, name: string): void {
    if (!confirm(`Are you sure you want to delete "${name}"?`)) {
      return;
    }

    this.isLoading.set(true);
    this.dataService.deleteSession(id).subscribe({
      next: (response) => {
        this.successMessage.set(
          response.message || "Session deleted successfully"
        );
        this.isLoading.set(false);
        this.loadSessions();
      },
      error: (err) => {
        console.error("Error deleting session:", err);
        this.errorMessage.set(err.error?.message || "Failed to delete session");
        this.isLoading.set(false);
      },
    });
  }

  formatDateForInput(dateString: string): string {
    const date = new Date(dateString);
    return date.toISOString().substring(0, 16);
  }

  formatDate(dateString: string): string {
    return new Date(dateString).toLocaleDateString();
  }
}
