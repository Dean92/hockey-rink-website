import { Routes } from "@angular/router";
import { Home } from "./home/home";
import { Login } from "./login/login";
import { Register } from "./register/register";
import { Leagues } from "./leagues/leagues";
import { Sessions } from "./sessions/sessions";
import { SessionRegistration } from "./session-registration/session-registration";
import { Profile } from "./profile/profile";
import { Dashboard } from "./dashboard/dashboard";
import { AuthGuard } from "./auth.guard";

export const routes: Routes = [
  { path: "", redirectTo: "/home", pathMatch: "full" },
  { path: "home", component: Home },
  { path: "login", component: Login },
  { path: "register", component: Register },
  { path: "leagues", component: Leagues },
  { path: "sessions", component: Sessions, canActivate: [AuthGuard] },
  {
    path: "session-registration",
    component: SessionRegistration,
    canActivate: [AuthGuard],
  },
  { path: "profile", component: Profile, canActivate: [AuthGuard] },
  { path: "dashboard", component: Dashboard, canActivate: [AuthGuard] },
];
