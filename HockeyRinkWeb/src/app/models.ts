export interface League {
  id: number;
  name: string;
  description: string;
  createdAt: Date;
  expectedStartDate?: string;
  preRegisterPrice?: number;
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
  leagueName?: string;
  registrations?: any[];
  maxPlayers?: number;
  registrationOpenDate?: string;
  registrationCloseDate?: string;
  earlyBirdPrice?: number;
  earlyBirdEndDate?: string;
  regularPrice?: number;
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
