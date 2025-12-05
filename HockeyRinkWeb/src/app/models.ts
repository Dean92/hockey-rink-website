export interface League {
  id: number;
  name: string;
  description: string;
  createdAt: Date;
  startDate?: string;
  earlyBirdPrice?: number;
  earlyBirdEndDate?: string;
  regularPrice?: number;
  registrationOpenDate?: string;
  registrationCloseDate?: string;
  teamCount?: number;
  // Legacy fields - keep for backward compatibility
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
  registrationCount?: number;
  spotsLeft?: number;
  isFull?: boolean;
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

export interface SessionRegistrationRequest {
  sessionId: number;
  name: string;
  address?: string;
  city?: string;
  state?: string;
  zipCode?: string;
  phone?: string;
  email: string;
  dateOfBirth: string;
  position?: string;
}
