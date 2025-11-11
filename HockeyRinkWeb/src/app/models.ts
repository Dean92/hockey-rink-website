export interface League {
  id: number;
  name: string;
  description: string;
  createdAt: Date;
}

export interface Session {
  id: number;
  name: string;
  startDate: string;
  endDate: string;
  fee: number;
  isActive: boolean;
  createdAt: string;
  leagueId?: number;
  league?: League;
  registrations?: any[];
}

export interface UserProfile {
  email: string;
  firstName: string;
  lastName: string;
  leagueId?: number;
}

export interface DashboardData {
  user: UserProfile;
  registeredSessions: RegisteredSession[];
}

export interface RegisteredSession {
  sessionId: number;
  sessionName: string;
  sessionStartDate: Date;
  sessionEndDate: Date;
  sessionFee: number;
  leagueName?: string;
}
