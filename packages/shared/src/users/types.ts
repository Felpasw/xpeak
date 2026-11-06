import type { Backgrounds } from '../auth/types';

/**
 * Public projection of a user, safe to hand to any authenticated
 * viewer. Mirrors the backend `PublicUserResponse`: drops the
 * private email but keeps progression and streak counters so the
 * public card can show the same tabs the owner sees on `/profile`.
 */
export interface PublicUser {
  id: string;
  username: string;
  avatarUrl: string | null;
  level: number;
  xp: number;
  currentStreakDays: number;
  longestStreakDays: number;
  background: Backgrounds | null;
  createdAt: string;
}
