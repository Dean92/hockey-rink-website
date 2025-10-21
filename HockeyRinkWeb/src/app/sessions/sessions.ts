import { Component, OnInit, signal } from "@angular/core";
import { DataService } from "../data";
import { CommonModule, DatePipe } from "@angular/common";
import { FormBuilder, FormGroup, ReactiveFormsModule } from "@angular/forms";
import { RouterLink } from "@angular/router";
import { tap, catchError, of } from "rxjs";
import { Session } from "../models";

@Component({
  selector: "app-sessions",
  standalone: true,
  imports: [CommonModule, DatePipe, ReactiveFormsModule, RouterLink],
  templateUrl: "./sessions.html",
  styleUrls: ["./sessions.css"],
})
export class Sessions implements OnInit {
  leagues = signal<any[]>([]);
  sessions = signal<Session[]>([]);
  errorMessage = signal<string | null>(null);
  isLoading = signal<boolean>(true);
  filterForm: FormGroup;

  constructor(private dataService: DataService, private fb: FormBuilder) {
    this.filterForm = this.fb.group({
      league: [""],
      date: [""],
    });
  }

  ngOnInit() {
    this.dataService
      .getLeagues()
      .pipe(
        tap((data) => {
          console.log("Leagues loaded:", data);
          this.leagues.set(data);
        }),
        catchError((err) => {
          console.error("Error fetching leagues:", err);
          this.errorMessage.set(
            err.error?.message || "Failed to fetch leagues. Please try again."
          );
          return of([]);
        })
      )
      .subscribe();

    this.applyFilters();

    this.filterForm.valueChanges
      .pipe(tap(() => this.applyFilters()))
      .subscribe();
  }

  applyFilters(): void {
    this.isLoading.set(true);
    const { league, date } = this.filterForm.value;
    const selectedLeagueId = league ? Number(league) : undefined;
    const selectedDate = date ? new Date(date) : undefined;

    console.log("Applying filters:", { selectedLeagueId, selectedDate });

    this.dataService
      .getSessions(selectedLeagueId, selectedDate)
      .pipe(
        tap((data) => {
          console.log("Sessions loaded:", data);
          this.sessions.set(data);
          this.errorMessage.set(null);
          this.isLoading.set(false);
        }),
        catchError((err) => {
          this.errorMessage.set(
            err.error?.message || "Failed to fetch sessions. Please try again."
          );
          console.error("Error fetching sessions:", err);
          this.isLoading.set(false);
          return of([]);
        })
      )
      .subscribe();
  }
}
