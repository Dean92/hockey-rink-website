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
import { AdminTeams } from './admin-teams/admin-teams';
import { AdminDraft } from './admin-draft/admin-draft';
import { PlayerDashboard } from './player-dashboard/player-dashboard';
import { SetupPassword } from './setup-password/setup-password';
import { NotFound } from './not-found/not-found';
import { AuthGuard } from './auth.guard';
import { AdminGuard } from './admin-guard';
import { PermissionGuard } from './permission-guard';
import { PublicSkate } from './public-skate/public-skate';
import { StickAndPuck } from './stick-and-puck/stick-and-puck';
import { AdultHockey } from './adult-hockey/adult-hockey';
import { YouthHockey } from './youth-hockey/youth-hockey';
import { RatHockey } from './rat-hockey/rat-hockey';
import { Classes } from './classes/classes';
import { AdminRinkCalendar } from './admin-rink-calendar/admin-rink-calendar';
import { AdminLeagueSchedule } from './admin-league-schedule/admin-league-schedule';
import { LeagueSchedule } from './league-schedule/league-schedule';
import { AdminUserManagement } from './admin-user-management/admin-user-management';

export const routes: Routes = [
  { path: '', redirectTo: '/home', pathMatch: 'full' },
  { path: 'home', component: Home },
  { path: 'login', component: Login },
  { path: 'register', component: Register },
  { path: 'leagues', component: Leagues },
  { path: 'sessions', component: Sessions },
  { path: 'public-skate', component: PublicSkate },
  { path: 'stick-and-puck', component: StickAndPuck },
  { path: 'adult-hockey', component: AdultHockey },
  { path: 'youth-hockey', component: YouthHockey },
  { path: 'rat-hockey', component: RatHockey },
  { path: 'classes', component: Classes },
  {
    path: 'session-registration',
    component: SessionRegistration,
    canActivate: [AuthGuard],
  },
  { path: 'profile', component: Profile, canActivate: [AuthGuard] },
  { path: 'dashboard', component: Dashboard, canActivate: [AuthGuard] },
  { path: 'my-teams', component: PlayerDashboard, canActivate: [AuthGuard] },
  // Admin dashboard: any admin role
  {
    path: 'admin',
    component: AdminDashboard,
    canActivate: [AuthGuard, AdminGuard],
  },
  // Manage Registrations permission
  {
    path: 'admin/users',
    component: AdminUsers,
    canActivate: [AuthGuard, PermissionGuard('manage-registrations')],
  },
  {
    path: 'admin/sessions',
    component: AdminSessions,
    canActivate: [AuthGuard, PermissionGuard('manage-registrations')],
  },
  // Manage Leagues permission
  {
    path: 'admin/leagues',
    component: AdminLeagues,
    canActivate: [AuthGuard, PermissionGuard('manage-leagues')],
  },
  {
    path: 'admin/sessions/:sessionId/teams',
    component: AdminTeams,
    canActivate: [AuthGuard, PermissionGuard('manage-leagues')],
  },
  {
    path: 'admin/sessions/:sessionId/draft',
    component: AdminDraft,
    canActivate: [AuthGuard, PermissionGuard('manage-leagues')],
  },
  {
    path: 'admin/leagues/:id/schedule',
    component: AdminLeagueSchedule,
    canActivate: [AuthGuard, PermissionGuard('manage-leagues')],
  },
  // Manage Schedule permission
  {
    path: 'admin/rink-calendar',
    component: AdminRinkCalendar,
    canActivate: [AuthGuard, PermissionGuard('manage-schedule')],
  },
  // Full Admin only
  {
    path: 'admin/user-management',
    component: AdminUserManagement,
    canActivate: [AuthGuard, AdminGuard],
  },
  { path: 'leagues/:id/schedule', component: LeagueSchedule },
  { path: 'setup-password/:token', component: SetupPassword },
  { path: '**', component: NotFound },
];
