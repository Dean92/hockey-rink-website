import { Component, OnInit } from "@angular/core";
import { AuthService } from "../auth";
import { DataService } from "../data";
import { Router } from "@angular/router";
import {
  ReactiveFormsModule,
  FormBuilder,
  FormGroup,
  Validators,
} from "@angular/forms";
import { CommonModule, DatePipe } from "@angular/common";

@Component({
  selector: "app-session-registration",
  imports: [CommonModule, ReactiveFormsModule, DatePipe],
  templateUrl: "./session-registration.html",
  styleUrl: "./session-registration.css",
})
export class SessionRegistration {
  sessions: any[] = [];
  errorMessage: string | null = null;
  registrationForm: FormGroup;

  constructor(
    private authService: AuthService,
    private dataService: DataService,
    private router: Router,
    private formBuilder: FormBuilder
  ) {
    this.registrationForm = this.formBuilder.group({
      sessionId: ["", Validators.required],
      // Add other form controls as needed
    });
  }

  ngOnInit() {
    this.checkAuthAndLoadSessions();
  }

  checkAuthAndLoadSessions() {
    this.authService.checkAuthStatus().subscribe({
      next: (authStatus) => {
        const isAuthenticated =
          authStatus.isValid ||
          authStatus.IsAuthenticated ||
          authStatus.isAuthenticated;

        if (isAuthenticated) {
          this.loadSessions();
        } else {
          this.errorMessage = "Not authenticated. Please login again.";
          this.router.navigate(["/login"]);
        }
      },
    });
  }

  loadSessions() {
    this.dataService.getSessions().subscribe({
      next: (data) => {
        console.log("Sessions fetched:", data);
        this.sessions = data;
      },
      error: (err) => {
        console.error("Error fetching sessions:", err);
        this.errorMessage =
          err.error?.message || "Failed to fetch sessions";
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
    this.errorMessage = "";
    if (this.registrationForm.invalid) {
      this.registrationForm.markAllAsTouched();
      return;
    }

    const sessionId = this.registrationForm.value.sessionId;
    this.dataService.registerSession(sessionId).subscribe({
      next: () => {
        console.log("Session registration successful");
        this.router.navigate(["/sessions"]);
      },
      error: (err) => {
        console.error("Session registration failed:", err);
        this.errorMessage =
          err.error?.message ||
          "Failed to register for session. Please try again.";
      },
    });
  }
}
