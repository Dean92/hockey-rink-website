import { Component, signal } from "@angular/core";
import { RouterOutlet, Router, RouterLink } from "@angular/router";
import { AuthService } from "./auth";
import { CommonModule } from "@angular/common";

@Component({
  selector: "app-root",
  imports: [RouterOutlet, RouterLink, CommonModule],
  templateUrl: "./app.html",
  styleUrl: "./app.css",
})
export class App {
  protected readonly title = signal("HockeyRinkWeb");

  constructor(private authService: AuthService, private router: Router) {}

  onLogout() {
    this.authService.logout();
    this.router.navigate(["/login"]);
  }
}
