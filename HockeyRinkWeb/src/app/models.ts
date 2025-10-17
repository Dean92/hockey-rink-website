export interface League {
  id: number;
  name: string;
  description: string;
  createdAt: string;
  teams?: Team[];
}

export interface Session {
  id: number;
  name: string;
  startDate: string;
  endDate: string;
  fee: number;
  isActive: boolean;
  createdAt: string;
  registrations: SessionRegistration[];
}

export interface Team {
  id: number;
  name: string;
  leagueId: number;
  createdAt: string;
}

export interface SessionRegistration {
  id: number;
  sessionId: number;
  userId: string;
  registeredAt: string;
  paymentStatus: string;
}
