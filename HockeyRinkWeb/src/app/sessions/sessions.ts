import { Component, OnInit, signal } from "@angular/core";
import { DataService } from "../data";
import { AuthService } from "../auth";
import { CommonModule, DatePipe } from "@angular/common";
import { FormBuilder, FormGroup, ReactiveFormsModule } from "@angular/forms";
import { tap, catchError, of } from "rxjs";

@Component({
  selector: "app-sessions",
  standalone: true,
  imports: [CommonModule, DatePipe, ReactiveFormsModule],
  templateUrl: "./sessions.html",
  styleUrls: ["./sessions.css"],
})
export class Sessions implements OnInit {
  leagues = signal<any[]>([]);
  sessions = signal<any[]>([]);
  errorMessage = signal<string | null>(null);
  filterForm: FormGroup;

  constructor(private dataService: DataService, private fb: FormBuilder) {
    this.filterForm = this.fb.group({
      league: [""],
      date: [""],
    });
  }

  ngOnInit() {
    // Load leagues for filter dropdown
    this.dataService
      .getLeagues()
      .pipe(
        tap((data) => this.leagues.set(data)),
        catchError((err) => {
          console.error("Error fetching leagues:", err);
          this.errorMessage.set(
            err.error?.message || "Failed to fetch leagues. Please try again."
          );
          return of([]);
        })
      )
      .subscribe();

    // Load initial sessions
    this.applyFilters();

    // Subscribe to filter form changes for reactive filtering
    this.filterForm.valueChanges
      .pipe(tap(() => this.applyFilters()))
      .subscribe();
  }

  applyFilters(): void {
    const { league, date } = this.filterForm.value;
    const selectedLeagueId = league ? Number(league) : undefined;
    const selectedDate = date ? new Date(date) : undefined;

    this.dataService
      .getSessions(selectedLeagueId, selectedDate)
      .pipe(
        tap((data) => {
          this.sessions.set(data);
          this.errorMessage.set(null);
        }),
        catchError((err) => {
          this.errorMessage.set(
            err.error?.message || "Failed to fetch sessions. Please try again."
          );
          console.error("Error fetching sessions:", err);
          return of([]);
        })
      )
      .subscribe();
  }
}
