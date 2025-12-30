import { Routes } from '@angular/router';
import { Home } from './home/home';
import { Login } from './login/login';
import { Register } from './register/register';
import { Leagues } from './leagues/leagues';
import { Sessions } from './sessions/sessions';
import { SessionRegistration } from './session-registration/session-registration';
import { Profile } from './profile/profile';
import { Dashboard } from './dashboard/dashboard';
import { AdminDashboard } from './admin-dashboard/admin-dashboard';
import { AdminUsers } from './admin-users/admin-users';
import { AdminSessions } from './admin-sessions/admin-sessions';
import { AdminLeagues } from './admin-leagues/admin-leagues';
import { SetupPassword } from './setup-password/setup-password';
import { NotFound } from './not-found/not-found';
import { AuthGuard } from './auth.guard';
import { AdminGuard } from './admin-guard';

export const routes: Routes = [
  { path: '', redirectTo: '/home', pathMatch: 'full' },
  { path: 'home', component: Home },
  { path: 'login', component: Login },
  { path: 'register', component: Register },
  { path: 'leagues', component: Leagues }, // Public - no auth required
  { path: 'sessions', component: Sessions }, // Public - no auth required
  {
    path: 'session-registration',
    component: SessionRegistration,
    canActivate: [AuthGuard],
  },
  { path: 'profile', component: Profile, canActivate: [AuthGuard] },
  { path: 'dashboard', component: Dashboard, canActivate: [AuthGuard] },
  {
    path: 'admin',
    component: AdminDashboard,
    canActivate: [AuthGuard, AdminGuard],
  },
  {
    path: 'admin/users',
    component: AdminUsers,
    canActivate: [AuthGuard, AdminGuard],
  },
  {
    path: 'admin/sessions',
    component: AdminSessions,
    canActivate: [AuthGuard, AdminGuard],
  },
  {
    path: 'admin/leagues',
    component: AdminLeagues,
    canActivate: [AuthGuard, AdminGuard],
  },
  { path: 'setup-password/:token', component: SetupPassword },
  { path: '**', component: NotFound }, // 404 - Wildcard route must be last
];
