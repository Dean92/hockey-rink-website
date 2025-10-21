import { Component, OnInit, signal } from "@angular/core";
import { DataService } from "../data";
import { CommonModule } from "@angular/common";
import { UserProfile } from "../models";

@Component({
  selector: "app-profile",
  standalone: true,
  imports: [CommonModule],
  templateUrl: "./profile.html",
  styleUrls: ["./profile.css"],
})
export class Profile implements OnInit {
  profile = signal<UserProfile | null>(null);
  errorMessage = signal<string | null>(null);
  isLoading = signal<boolean>(true);

  constructor(private dataService: DataService) {}

  ngOnInit() {
    this.loadProfile();
  }

  loadProfile() {
    this.dataService.getProfile().subscribe({
      next: (data) => {
        this.profile.set(data);
        this.isLoading.set(false);
      },
      error: (err) => {
        console.error("Error fetching profile:", err);
        this.errorMessage.set(
          err.error?.message || "Failed to fetch profile. Please try again."
        );
        this.isLoading.set(false);
      },
    });
  }
}
