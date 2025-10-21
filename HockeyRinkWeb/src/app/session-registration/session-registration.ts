import { Component, OnInit, signal } from "@angular/core";
import { AuthService } from "../auth";
import { DataService } from "../data";
import { Router, ActivatedRoute } from "@angular/router";
import {
  ReactiveFormsModule,
  FormBuilder,
  FormGroup,
  Validators,
} from "@angular/forms";
import { CommonModule, DatePipe } from "@angular/common";
import { Session } from "../models";

@Component({
  selector: "app-session-registration",
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, DatePipe],
  templateUrl: "./session-registration.html",
  styleUrl: "./session-registration.css",
})
export class SessionRegistration implements OnInit {
  sessions = signal<Session[]>([]);
  errorMessage = signal<string | null>(null);
  successMessage = signal<string | null>(null);
  isLoading = signal<boolean>(true);
  registrationForm: FormGroup;

  constructor(
    private authService: AuthService,
    private dataService: DataService,
    private router: Router,
    private route: ActivatedRoute,
    private formBuilder: FormBuilder
  ) {
    this.registrationForm = this.formBuilder.group({
      sessionId: ["", Validators.required],
    });
  }

  ngOnInit() {
    this.loadSessions();

    // Check if sessionId was passed via query params
    this.route.queryParams.subscribe((params) => {
      if (params["sessionId"]) {
        this.registrationForm.patchValue({ sessionId: params["sessionId"] });
      }
    });
  }

  loadSessions() {
    this.dataService.getSessions().subscribe({
      next: (data) => {
        console.log("Sessions fetched:", data);
        this.sessions.set(data.filter((s) => s.isActive)); // Only show active sessions
        this.isLoading.set(false);
      },
      error: (err) => {
        console.error("Error fetching sessions:", err);
        this.errorMessage.set(err.error?.message || "Failed to fetch sessions");
        this.isLoading.set(false);
      },
    });
  }

  hasError(field: string, errorType: string): boolean {
    const control = this.registrationForm.get(field);
    return !!(
      control &&
      control.hasError(errorType) &&
      (control.dirty || control.touched)
    );
  }

  onRegister() {
    this.errorMessage.set(null);
    this.successMessage.set(null);

    if (this.registrationForm.invalid) {
      this.registrationForm.markAllAsTouched();
      return;
    }

    this.isLoading.set(true);
    const sessionId = this.registrationForm.value.sessionId;

    this.dataService.registerSession(sessionId).subscribe({
      next: (response) => {
        console.log("Session registration successful:", response);
        this.successMessage.set("Successfully registered for session!");
        this.isLoading.set(false);

        // Redirect to dashboard after 2 seconds
        setTimeout(() => {
          this.router.navigate(["/dashboard"]);
        }, 2000);
      },
      error: (err) => {
        console.error("Session registration failed:", err);
        this.errorMessage.set(
          err.error?.message ||
            "Failed to register for session. Please try again."
        );
        this.isLoading.set(false);
      },
    });
  }
}
