export interface User {
  id: string;
  microsoftId: string;
  displayName: string;
  email: string;
  jobTitle?: string;
  department?: string;
  lastSynced: string;
}

export interface CalendarEvent {
  id: string;
  microsoftId: string;
  subject: string;
  startTime: string;
  endTime: string;
  location?: string;
  isAllDay: boolean;
  userId: string;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  pageSize: number;
  currentPage: number;
  totalPages: number;
  hasNext: boolean;
  hasPrevious: boolean;
  onlyWithEvents?: boolean;
}

export interface LoginData {
  email: string;
  password: string;
}

export interface AuthResponse {
  token: string;
  expiration: string;
  user: User;
}


