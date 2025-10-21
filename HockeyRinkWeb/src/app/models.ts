export interface League {
  id: number;
  name: string;
  description: string;
  createdAt: Date;
}

export interface Session {
  id: number;
  name: string;
  startDate: Date;
  endDate: Date;
  fee: number;
  isActive: boolean;
  createdAt: Date;
  leagueId?: number;
  league?: League;
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
