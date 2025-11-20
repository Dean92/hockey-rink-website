import { Component, OnInit, signal } from "@angular/core";
import { CommonModule } from "@angular/common";
import {
  ReactiveFormsModule,
  FormBuilder,
  FormGroup,
  Validators,
} from "@angular/forms";
import { DataService } from "../data";
import { League } from "../models";

@Component({
  selector: "app-admin-leagues",
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: "./admin-leagues.html",
  styleUrls: ["./admin-leagues.css"],
})
export class AdminLeagues implements OnInit {
  leagues = signal<League[]>([]);
  errorMessage = signal<string | null>(null);
  successMessage = signal<string | null>(null);
  isLoading = signal<boolean>(false);
  showModal = signal<boolean>(false);
  isEditMode = signal<boolean>(false);
  leagueForm: FormGroup;
  currentLeagueId: number | null = null;

  constructor(private dataService: DataService, private fb: FormBuilder) {
    this.leagueForm = this.fb.group({
      name: ["", [Validators.required, Validators.maxLength(100)]],
      description: [""],
      startDate: [""],
      earlyBirdPrice: [null, [Validators.min(0), Validators.max(1000)]],
      earlyBirdEndDate: [""],
      regularPrice: [null, [Validators.min(0), Validators.max(1000)]],
      registrationOpenDate: [""],
      registrationCloseDate: [""],
    });
  }

  ngOnInit(): void {
    this.loadLeagues();
  }

  loadLeagues(): void {
    this.isLoading.set(true);
    this.dataService.getAllLeagues().subscribe({
      next: (data) => {
        this.leagues.set(data);
        this.isLoading.set(false);
      },
      error: (err) => {
        console.error("Error loading leagues:", err);
        this.errorMessage.set("Failed to load leagues");
        this.isLoading.set(false);
      },
    });
  }

  openEditModal(league: League): void {
    this.isEditMode.set(true);
    this.currentLeagueId = league.id;
    this.leagueForm.patchValue({
      name: league.name,
      description: league.description || "",
      startDate: league.startDate
        ? this.formatDateForInput(league.startDate)
        : "",
      earlyBirdPrice: league.earlyBirdPrice,
      earlyBirdEndDate: league.earlyBirdEndDate
        ? this.formatDateTimeForInput(league.earlyBirdEndDate)
        : "",
      regularPrice: league.regularPrice,
      registrationOpenDate: league.registrationOpenDate
        ? this.formatDateTimeForInput(league.registrationOpenDate)
        : "",
      registrationCloseDate: league.registrationCloseDate
        ? this.formatDateTimeForInput(league.registrationCloseDate)
        : "",
    });
    this.showModal.set(true);
    this.errorMessage.set(null);
    this.successMessage.set(null);
  }

  closeModal(): void {
    this.showModal.set(false);
    this.leagueForm.reset();
    this.currentLeagueId = null;
  }

  onSubmit(): void {
    if (this.leagueForm.invalid) {
      this.leagueForm.markAllAsTouched();
      return;
    }

    this.isLoading.set(true);
    this.errorMessage.set(null);
    this.successMessage.set(null);

    const formData = this.leagueForm.value;

    // Convert empty strings to null for datetime fields
    const cleanedData = {
      ...formData,
      startDate: formData.startDate || null,
      earlyBirdPrice: formData.earlyBirdPrice || null,
      earlyBirdEndDate: formData.earlyBirdEndDate || null,
      regularPrice: formData.regularPrice || null,
      registrationOpenDate: formData.registrationOpenDate || null,
      registrationCloseDate: formData.registrationCloseDate || null,
    };

    if (this.isEditMode() && this.currentLeagueId) {
      // Update existing league
      this.dataService
        .updateLeague(this.currentLeagueId, cleanedData)
        .subscribe({
          next: (response) => {
            this.successMessage.set(
              response.message || "League updated successfully"
            );
            this.isLoading.set(false);
            this.closeModal();
            this.loadLeagues();
          },
          error: (err) => {
            console.error("Error updating league:", err);
            const leagueName = this.leagueForm.get("name")?.value;
            this.errorMessage.set(
              err.error?.message || `Failed to update league "${leagueName}"`
            );
            this.isLoading.set(false);
          },
        });
    }
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

  formatDate(dateString: string | null | undefined): string {
    if (!dateString) return "Not Set";
    return new Date(dateString).toLocaleDateString();
  }

  formatDateTime(dateString: string | null | undefined): string {
    if (!dateString) return "Not Set";
    return new Date(dateString).toLocaleString();
  }

  formatCurrency(value: number | null | undefined): string {
    if (value === null || value === undefined) return "Not Set";
    return `$${value.toFixed(2)}`;
  }
}
