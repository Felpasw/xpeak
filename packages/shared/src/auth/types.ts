export interface User {
  id: string;
  username: string;
  email: string;
  avatarUrl: string | null;
  level: number;
  xp: number;
  currentStreakDays: number;
  longestStreakDays: number;
  createdAt: string;
}

export interface AuthResponse {
  user: User;
}
