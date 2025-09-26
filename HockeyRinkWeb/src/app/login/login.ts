import { Component } from "@angular/core";
import { AuthService } from "../auth";
import { Router } from "@angular/router";
import { FormsModule } from "@angular/forms";
import { CommonModule } from "@angular/common";

@Component({
  selector: "app-login",
  imports: [FormsModule, CommonModule],
  templateUrl: "./login.html",
  styleUrls: ["./login.css"],
})
export class Login {
  email: string = "";
  password: string = "";

  constructor(private authService: AuthService, private router: Router) {}

  onLogin() {
    this.authService.login(this.email, this.password).subscribe({
      next: (response) => {
        this.router.navigate(["/leagues"]);
      },
      error: (err) => {
        console.error("Login failed", err);
      },
    });
  }
}
