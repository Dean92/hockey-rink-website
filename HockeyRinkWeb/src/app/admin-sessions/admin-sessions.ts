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
  showDeleteModal = signal<boolean>(false);
  isEditMode = signal<boolean>(false);
  sessionForm: FormGroup;
  currentSessionId: number | null = null;
  sessionToDelete: { id: number; name: string } | null = null;

  constructor(private dataService: DataService, private fb: FormBuilder) {
    this.sessionForm = this.fb.group({
      name: ["", [Validators.required, Validators.maxLength(100)]],
      startDate: ["", Validators.required],
      endDate: ["", Validators.required],
      fee: [0],
      isActive: [true],
      leagueId: ["", Validators.required],
      maxPlayers: [
        20,
        [Validators.required, Validators.min(1), Validators.max(250)],
      ],
      registrationOpenDate: [""],
      registrationCloseDate: [""],
      earlyBirdPrice: [null, [Validators.min(0), Validators.max(1000)]],
      earlyBirdEndDate: [""],
      regularPrice: [
        0,
        [Validators.required, Validators.min(0), Validators.max(1000)],
      ],
    });

    // Auto-populate fee from regular price
    this.sessionForm.get("regularPrice")?.valueChanges.subscribe((value) => {
      this.sessionForm.patchValue({ fee: value || 0 }, { emitEvent: false });
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
      maxPlayers: 20,
      registrationOpenDate: "",
      registrationCloseDate: "",
      earlyBirdPrice: null,
      earlyBirdEndDate: "",
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
        : "",
      registrationCloseDate: session.registrationCloseDate
        ? this.formatDateTimeForInput(session.registrationCloseDate)
        : "",
      earlyBirdPrice: session.earlyBirdPrice,
      earlyBirdEndDate: session.earlyBirdEndDate
        ? this.formatDateTimeForInput(session.earlyBirdEndDate)
        : "",
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

    // Convert empty strings to null for datetime fields
    const cleanedData = {
      ...formData,
      registrationOpenDate: formData.registrationOpenDate || null,
      registrationCloseDate: formData.registrationCloseDate || null,
      earlyBirdEndDate: formData.earlyBirdEndDate || null,
    };

    // Log the datetime values being sent
    console.log("Form data being submitted:", {
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
              response.message || "Session updated successfully"
            );
            this.isLoading.set(false);
            this.closeModal();
            this.loadSessions();
          },
          error: (err) => {
            console.error("Error updating session:", err);
            const sessionName = this.sessionForm.get("name")?.value;
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
            response.message || "Session created successfully"
          );
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
        console.error("Error deleting session:", err);
        this.errorMessage.set(err.error?.message || "Failed to delete session");
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
    const month = String(date.getMonth() + 1).padStart(2, "0");
    const day = String(date.getDate()).padStart(2, "0");
    return `${year}-${month}-${day}`;
  }

  formatDateTimeForInput(dateString: string): string {
    const date = new Date(dateString);
    // Return YYYY-MM-DDTHH:mm format for datetime-local inputs in local time
    const year = date.getFullYear();
    const month = String(date.getMonth() + 1).padStart(2, "0");
    const day = String(date.getDate()).padStart(2, "0");
    const hours = String(date.getHours()).padStart(2, "0");
    const minutes = String(date.getMinutes()).padStart(2, "0");
    return `${year}-${month}-${day}T${hours}:${minutes}`;
  }

  formatDate(dateString: string): string {
    return new Date(dateString).toLocaleDateString();
  }
}
