import { Routes } from "@angular/router";
import { Home } from "./home/home";
import { Login } from "./login/login";
import { Register } from "./register/register";
import { Leagues } from "./leagues/leagues";

export const routes: Routes = [
  { path: "", redirectTo: "/home", pathMatch: "full" },
  { path: "home", component: Home },
  { path: "login", component: Login },
  { path: "register", component: Register },
  { path: "leagues", component: Leagues },
];
